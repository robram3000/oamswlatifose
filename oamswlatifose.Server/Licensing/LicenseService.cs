using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using oamswlatifose.Server.Services.Email.Interfaces;

namespace oamswlatifose.Server.Licensing
{
    public interface ILicenseService
    {
        LicenseState GetCurrentState();
        bool IsAccessAllowed();
        (bool success, string message) ActivateLicense(string licenseKey);

        /// <summary>Creates a duration-based license request and emails the owner a confirmation link.</summary>
        Task<(bool success, string message)> RequestLicenseAsync(string durationKey, string? requester, string confirmBaseUrl);

        /// <summary>Owner-confirms a pending request (from the emailed link); activates the duration.</summary>
        (bool success, string message, DateTime? expiry) ConfirmLicense(string id, string token);
    }

    public class LicenseService : ILicenseService
    {
        private const int TrialDays = 30;
        private const string LicenseAuthor = "robram3000@gmail.com";
        private const string CacheKey = "license_state";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        // RSA-2048 public key — only Roberto V Ramirez Jr (robram3000@gmail.com) holds the matching private key.
        private const string PublicKeyPem = """
            -----BEGIN PUBLIC KEY-----
            MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAomCPr0VWrSluYbbshpwH
            /tp6alL4Mt+R5pjpLddI2DkCM93wkXuCua0ZmI+pIqC/c6kPs+2oF/eYqzMmEjpT
            yTE96hA3WoXO+jo7EhtMmbz4/NSkrlcKq016sFwE2S6LNkfp0RANeLXS2m2THTbj
            DfUva7WQaYnPIlA2i/pVwEwYiWP9mLeaBkCWz9um8dkp7bRry0McomPPVLU275Q9
            gDXa88srymdF5OzfBZ8oU7e8HwcFcc2Ykpr+MhO1OHH8+Ld3B2FaSiSNVO3rfTdh
            A+nmCifjvxsf/pLcAVIPXArbi7fsADSfeGQoBD9nHStmYVh9VCQph8SXGAkh/VbI
            BQIDAQAB
            -----END PUBLIC KEY-----
            """;

        // Selectable license durations offered in the UI (label shown to the user).
        private static readonly Dictionary<string, (int Days, string Label)> Durations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["1month"] = (30, "1 Month"),
            ["1year"] = (365, "1 Year"),
            ["2year"] = (730, "2 Years"),
        };

        private readonly ILicensePersistence _persistence;
        private readonly IMemoryCache _cache;
        // LicenseService is a singleton; IEmailService is scoped, so resolve it per-call from a scope.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(ILicensePersistence persistence, IMemoryCache cache, IServiceScopeFactory scopeFactory, ILogger<LicenseService> logger)
        {
            _persistence = persistence;
            _cache = cache;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public LicenseState GetCurrentState()
        {
            if (_cache.TryGetValue(CacheKey, out LicenseState? cached) && cached is not null)
                return cached;

            var state = ComputeState();
            _cache.Set(CacheKey, state, CacheTtl);
            return state;
        }

        public bool IsAccessAllowed()
        {
            var s = GetCurrentState();
            return s.Status is LicenseStatus.Trial or LicenseStatus.Licensed;
        }

        public (bool success, string message) ActivateLicense(string licenseKey)
        {
            var payload = VerifyKey(licenseKey);
            if (payload is null)
                return (false, $"Invalid license key. Contact {LicenseAuthor} for a valid license.");

            if (payload.ExpiresAt <= DateTime.UtcNow)
                return (false, $"This license key has already expired. Contact {LicenseAuthor} to renew.");

            var deployment = _persistence.Load();
            deployment.LicenseKey = licenseKey;
            _persistence.Save(deployment);
            _cache.Remove(CacheKey);

            _logger.LogInformation("License activated for {Licensee}, expires {Expiry:yyyy-MM-dd}", payload.Licensee, payload.ExpiresAt);
            return (true, $"License activated. Licensed to: {payload.Licensee}. Valid until: {payload.ExpiresAt:yyyy-MM-dd}.");
        }

        public async Task<(bool success, string message)> RequestLicenseAsync(string durationKey, string? requester, string confirmBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(durationKey) || !Durations.TryGetValue(durationKey, out var duration))
                return (false, "Please choose a valid license duration.");

            var deployment = _persistence.Load();

            var pending = new PendingLicenseRequest
            {
                Id = Guid.NewGuid().ToString("N"),
                Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
                DurationKey = durationKey,
                DurationDays = duration.Days,
                DurationLabel = duration.Label,
                Requester = string.IsNullOrWhiteSpace(requester) ? "a deployment" : requester.Trim(),
                RequestedAt = DateTime.UtcNow,
            };
            deployment.Pending = pending;
            _persistence.Save(deployment);

            var confirmUrl = $"{confirmBaseUrl.TrimEnd('/')}/api/license/confirm?id={pending.Id}&token={pending.Token}";
            var emailBody = BuildRequestEmail(pending, confirmUrl);

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var send = await emailService.SendHtmlEmailAsync(
                LicenseAuthor, $"License activation request — {pending.DurationLabel}", emailBody);

            if (!send.IsSuccess)
            {
                _logger.LogWarning("License request email failed: {Msg}", send.Message);
                return (false, $"Could not send the activation request email — {send.Message}");
            }

            _logger.LogInformation("License request ({Duration}) from {Requester} emailed to {Owner} for confirmation.",
                pending.DurationLabel, pending.Requester, LicenseAuthor);
            return (true, $"Activation request for {pending.DurationLabel} sent to {LicenseAuthor}. Access begins once it is confirmed.");
        }

        public (bool success, string message, DateTime? expiry) ConfirmLicense(string id, string token)
        {
            var deployment = _persistence.Load();
            var pending = deployment.Pending;

            if (pending is null || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token))
                return (false, "No pending license request to confirm.", null);

            // Constant-time compare on the token to avoid leaking it via timing.
            var idOk = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(pending.Id), Encoding.UTF8.GetBytes(id));
            var tokenOk = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(pending.Token), Encoding.UTF8.GetBytes(token));
            if (!idOk || !tokenOk)
                return (false, "This confirmation link is invalid or has already been used.", null);

            var expiry = DateTime.UtcNow.AddDays(pending.DurationDays);
            deployment.GrantedExpiry = expiry;
            deployment.GrantedTo = pending.Requester;
            deployment.Pending = null;       // single-use
            _persistence.Save(deployment);
            _cache.Remove(CacheKey);

            _logger.LogInformation("License confirmed for {Requester} ({Duration}); valid until {Expiry:yyyy-MM-dd}.",
                pending.Requester, pending.DurationLabel, expiry);
            return (true, $"License activated for {pending.Requester} — {pending.DurationLabel}, valid until {expiry:yyyy-MM-dd}.", expiry);
        }

        private LicenseState ComputeState()
        {
            var deployment = _persistence.Load();
            var now = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(deployment.LicenseKey))
            {
                var payload = VerifyKey(deployment.LicenseKey);
                if (payload is not null)
                {
                    return payload.ExpiresAt > now
                        ? new LicenseState
                        {
                            Status = LicenseStatus.Licensed,
                            FirstRunDate = deployment.FirstRunDate,
                            ExpiryDate = payload.ExpiresAt,
                            LicensedTo = payload.Licensee
                        }
                        : new LicenseState
                        {
                            Status = LicenseStatus.Expired,
                            FirstRunDate = deployment.FirstRunDate,
                            ExpiryDate = payload.ExpiresAt,
                            LicensedTo = payload.Licensee
                        };
                }
            }

            // Owner-confirmed duration grant (the email-confirm flow).
            if (deployment.GrantedExpiry.HasValue)
            {
                return new LicenseState
                {
                    Status = deployment.GrantedExpiry.Value > now ? LicenseStatus.Licensed : LicenseStatus.Expired,
                    FirstRunDate = deployment.FirstRunDate,
                    ExpiryDate = deployment.GrantedExpiry,
                    LicensedTo = deployment.GrantedTo,
                };
            }

            var trialEnd = deployment.FirstRunDate.AddDays(TrialDays);
            if (now <= trialEnd)
            {
                return new LicenseState
                {
                    Status = LicenseStatus.Trial,
                    FirstRunDate = deployment.FirstRunDate,
                    ExpiryDate = trialEnd,
                    TrialDaysRemaining = (int)Math.Ceiling((trialEnd - now).TotalDays)
                };
            }

            return new LicenseState
            {
                Status = LicenseStatus.Expired,
                FirstRunDate = deployment.FirstRunDate,
                ExpiryDate = trialEnd
            };
        }

        private LicensePayload? VerifyKey(string licenseKey)
        {
            try
            {
                var dot = licenseKey.IndexOf('.');
                if (dot < 1 || dot == licenseKey.Length - 1) return null;

                var payloadB64 = licenseKey[..dot];
                var sigB64 = licenseKey[(dot + 1)..];

                var payloadBytes = Convert.FromBase64String(PadBase64(payloadB64));
                var signatureBytes = Convert.FromBase64String(PadBase64(sigB64));

                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson);
                if (payload is null || payload.IssuedBy != LicenseAuthor) return null;

                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicKeyPem);
                return rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                    ? payload
                    : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "License key verification failed");
                return null;
            }
        }

        private static string BuildRequestEmail(PendingLicenseRequest p, string confirmUrl)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#202124'>
  <h2 style='margin:0 0 8px'>License activation request</h2>
  <p style='color:#5f6368;margin:0 0 16px'>A deployment of the Aglipay Attendance system is requesting a license.</p>
  <table style='border-collapse:collapse;font-size:14px;margin-bottom:20px'>
    <tr><td style='padding:4px 12px 4px 0;color:#5f6368'>Requested by</td><td><strong>{System.Net.WebUtility.HtmlEncode(p.Requester)}</strong></td></tr>
    <tr><td style='padding:4px 12px 4px 0;color:#5f6368'>Duration</td><td><strong>{p.DurationLabel}</strong> ({p.DurationDays} days)</td></tr>
    <tr><td style='padding:4px 12px 4px 0;color:#5f6368'>Requested at</td><td>{p.RequestedAt:yyyy-MM-dd HH:mm} UTC</td></tr>
  </table>
  <a href='{confirmUrl}' style='display:inline-block;background:#1a73e8;color:#fff;text-decoration:none;padding:12px 24px;border-radius:6px;font-weight:600'>
    Confirm &amp; activate {p.DurationLabel}
  </a>
  <p style='color:#80868b;font-size:12px;margin-top:18px'>
    Clicking confirm activates the license immediately and the {p.DurationLabel.ToLower()} duration starts now.
    If you didn&#39;t expect this, ignore this email and the request stays inactive.
  </p>
</div>";
        }

        private static string PadBase64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            int pad = (4 - s.Length % 4) % 4;
            return pad == 0 ? s : s + new string('=', pad);
        }
    }
}
