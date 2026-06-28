using System.Text.Json.Serialization;

namespace oamswlatifose.Server.Licensing
{
    public class LicensePayload
    {
        [JsonPropertyName("Licensee")]
        public string Licensee { get; set; } = string.Empty;

        [JsonPropertyName("IssuedBy")]
        public string IssuedBy { get; set; } = string.Empty;

        [JsonPropertyName("IssuedAt")]
        public DateTime IssuedAt { get; set; }

        [JsonPropertyName("ExpiresAt")]
        public DateTime ExpiresAt { get; set; }
    }

    public enum LicenseStatus
    {
        Trial,
        Licensed,
        Expired,
        Invalid
    }

    public class LicenseState
    {
        public LicenseStatus Status { get; set; }
        public DateTime? FirstRunDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? LicensedTo { get; set; }
        public int TrialDaysRemaining { get; set; }
    }

    public class DeploymentState
    {
        [JsonPropertyName("firstRunDate")]
        public DateTime FirstRunDate { get; set; }

        [JsonPropertyName("licenseKey")]
        public string? LicenseKey { get; set; }

        /// <summary>Expiry of an owner-confirmed license grant (the email-confirm flow). Null if none.</summary>
        [JsonPropertyName("grantedExpiry")]
        public DateTime? GrantedExpiry { get; set; }

        [JsonPropertyName("grantedTo")]
        public string? GrantedTo { get; set; }

        /// <summary>A license request awaiting the owner's email confirmation (one at a time).</summary>
        [JsonPropertyName("pending")]
        public PendingLicenseRequest? Pending { get; set; }
    }

    /// <summary>A duration-based license request the owner must confirm via an emailed link.</summary>
    public class PendingLicenseRequest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("durationKey")]
        public string DurationKey { get; set; } = string.Empty;

        [JsonPropertyName("durationDays")]
        public int DurationDays { get; set; }

        [JsonPropertyName("durationLabel")]
        public string DurationLabel { get; set; } = string.Empty;

        [JsonPropertyName("requester")]
        public string? Requester { get; set; }

        [JsonPropertyName("requestedAt")]
        public DateTime RequestedAt { get; set; }
    }

    public record ActivateLicenseRequest(string LicenseKey);

    /// <summary>Body for POST /api/license/request — a duration choice (1month / 1year / 2year).</summary>
    public record RequestLicenseRequest(string DurationKey, string? Requester);
}
