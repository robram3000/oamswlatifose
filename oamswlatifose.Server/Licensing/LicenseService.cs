using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace oamswlatifose.Server.Licensing
{
    public interface ILicenseService
    {
        LicenseState GetCurrentState();
        bool IsAccessAllowed();
        (bool success, string message) ActivateLicense(string licenseKey);
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

        private readonly ILicensePersistence _persistence;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LicenseService> _logger;

        public LicenseService(ILicensePersistence persistence, IMemoryCache cache, ILogger<LicenseService> logger)
        {
            _persistence = persistence;
            _cache = cache;
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

        private static string PadBase64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            int pad = (4 - s.Length % 4) % 4;
            return pad == 0 ? s : s + new string('=', pad);
        }
    }
}
