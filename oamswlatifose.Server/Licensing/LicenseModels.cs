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
    }

    public record ActivateLicenseRequest(string LicenseKey);
}
