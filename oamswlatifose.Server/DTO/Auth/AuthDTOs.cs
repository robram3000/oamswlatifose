using oamswlatifose.Server.DTO.User;
using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Auth
{
    /// <summary>
    /// DTO for user login request.
    /// </summary>
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// DTO for login response with authentication tokens.
    /// </summary>
    public class LoginResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserResponseDTO User { get; set; }
    }

    /// <summary>
    /// DTO for user registration request.
    /// </summary>
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        public int? EmployeeId { get; set; }
    }

    /// <summary>
    /// DTO for token refresh request.
    /// </summary>
    public class RefreshTokenRequestDTO
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// DTO for token refresh response.
    /// </summary>
    public class RefreshTokenResponseDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO for email verification request.
    /// </summary>
    public class VerifyEmailDTO
    {
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Verification token is required")]
        public string Token { get; set; }
    }

    /// <summary>
    /// DTO for two-factor authentication verification.
    /// </summary>
    public class TwoFactorDTO
    {
        [Required(ErrorMessage = "User ID is required")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must contain only numbers")]
        public string Code { get; set; }
    }

    /// <summary>
    /// DTO for authentication context used internally.
    /// </summary>
    public class AuthContextDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string DeviceType { get; set; }
        public string Location { get; set; }
        public DateTime LoginTime { get; set; }
        public string CorrelationId { get; set; }
    }
}
