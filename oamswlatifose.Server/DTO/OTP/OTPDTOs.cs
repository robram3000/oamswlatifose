using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.OTP
{
    /// <summary>
    /// OTP purpose enumeration for different verification workflows
    /// </summary>
    public enum OTPPurpose
    {
        EmailVerification = 1,
        PasswordReset = 2,
        TwoFactorAuthentication = 3,
        EmailChange = 4
    }

    /// <summary>
    /// Request DTO for email-based OTP generation
    /// </summary>
    public class EmailOTPRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        public OTPPurpose Purpose { get; set; } = OTPPurpose.EmailVerification;
    }

    /// <summary>
    /// Request DTO for two-factor authentication OTP
    /// </summary>
    public class TwoFactorOTPRequestDTO
    {
        public OTPPurpose Purpose { get; set; } = OTPPurpose.TwoFactorAuthentication;
    }

    /// <summary>
    /// Request DTO for OTP verification
    /// </summary>
    public class OTPVerificationRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(8, MinimumLength = 6, ErrorMessage = "OTP code must be between 6 and 8 digits")]
        [RegularExpression(@"^\d+$", ErrorMessage = "OTP code must contain only digits")]
        public string OTPCode { get; set; }

        public OTPPurpose Purpose { get; set; }
    }

    /// <summary>
    /// Response DTO for OTP generation
    /// </summary>
    public class OTPGenerationResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int ExpiresInMinutes { get; set; }
        public string EmailMasked { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response DTO for OTP verification
    /// </summary>
    public class OTPVerificationResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Email { get; set; }
        public OTPPurpose Purpose { get; set; }
        public string ResetToken { get; set; } // For password reset flow
        public bool EmailVerified { get; set; } // For email verification
        public bool TwoFactorAuthenticated { get; set; } // For 2FA
    }

    /// <summary>
    /// Request DTO for resending OTP
    /// </summary>
    public class ResendOTPRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        public OTPPurpose Purpose { get; set; }
    }

    /// <summary>
    /// Request DTO for invalidating OTPs
    /// </summary>
    public class InvalidateOTPRequestDTO
    {
        public string Email { get; set; }
    }

    /// <summary>
    /// DTO for email verification status
    /// </summary>
    public class EmailVerificationStatusDTO
    {
        public string Email { get; set; }
        public bool IsVerified { get; set; }
        public string Message { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

    /// <summary>
    /// Error response DTO
    /// </summary>
    public class ErrorResponseDTO
    {
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
