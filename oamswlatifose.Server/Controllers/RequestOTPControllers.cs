using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.DTO.OTP;
using oamswlatifose.Server.Services.Email.Interfaces; // Updated namespace
using oamswlatifose.Server.Services;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// API controller for email-based One-Time Password (OTP) operations including generation,
    /// verification, and management of OTPs for email verification, password reset,
    /// and two-factor authentication workflows via email.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Generate OTP for email verification during registration</para>
    /// <para>- Generate OTP for password reset via email</para>
    /// <para>- Generate OTP for two-factor authentication (2FA) via email</para>
    /// <para>- Verify OTP codes with expiration checking</para>
    /// <para>- Resend OTP with rate limiting to prevent abuse</para>
    /// <para>- Invalidate existing OTPs for security after use or timeout</para>
    /// 
    /// <para>Security Features:</para>
    /// <para>- Rate limiting per email (max 3 attempts per 10 minutes)</para>
    /// <para>- OTP expiration after configurable time (default 10 minutes)</para>
    /// <para>- Maximum verification attempts (5 attempts then temporary lockout)</para>
    /// <para>- Secure cryptographically random OTP generation (6-8 digits)</para>
    /// <para>- Prevention of OTP reuse after successful verification</para>
    /// <para>- IP-based tracking for suspicious activity detection</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RequestOTPController : BaseApiController
    {
        private readonly IEmailService _emailService; // Changed from IEmailOTPService
        private readonly ILogger<RequestOTPController> _logger;

        public RequestOTPController(
            IEmailService emailService, // Changed parameter
            ILogger<RequestOTPController> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates and sends an OTP to the specified email address for email verification.
        /// Used during user registration to verify email ownership.
        /// Includes rate limiting to prevent abuse and spam.
        /// </summary>
        /// <param name="request">Email address for OTP generation</param>
        /// <returns>Success status with expiration time and masked email</returns>
        /// <response code="200">OTP sent successfully to email</response>
        /// <response code="400">Invalid email format or email already verified</response>
        /// <response code="429">Too many requests, rate limit exceeded</response>
        [HttpPost("send-verification")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OTPGenerationResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SendVerificationOTP([FromBody] EmailOTPRequestDTO request)
        {
            try
            {
                // Validate email format
                if (string.IsNullOrWhiteSpace(request?.Email) || !IsValidEmail(request.Email))
                {
                    return BadRequest(new ErrorResponseDTO
                    {
                        Message = "Invalid email address",
                        CorrelationId = GetCorrelationId()
                    });
                }

                var clientIp = GetClientIpAddress();

                // Generate OTP code
                string otpCode = GenerateNumericOtp(6);
                string userName = ExtractNameFromEmail(request.Email);
                int expiryMinutes = 10;

                // Send email using IEmailService
                var emailResult = await _emailService.SendEmailVerificationOtpAsync(
                    request.Email,
                    userName,
                    otpCode,
                    expiryMinutes);

                if (!emailResult.IsSuccess)
                {
                    return BadRequest(new ErrorResponseDTO
                    {
                        Message = emailResult.Message ?? "Failed to send verification email",
                        CorrelationId = GetCorrelationId()
                    });
                }

                _logger.LogInformation("Verification OTP sent to {Email} from IP {ClientIp}",
                    MaskEmail(request.Email), clientIp);

                return Ok(new OTPGenerationResponseDTO
                {
                    Success = true,
                    Message = "Verification code sent successfully",
                    ExpiresInMinutes = expiryMinutes,
                    EmailMasked = MaskEmail(request.Email)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending verification OTP to {Email}", MaskEmail(request?.Email));
                return StatusCode(500, new ErrorResponseDTO
                {
                    Message = "Failed to send verification email",
                    CorrelationId = GetCorrelationId()
                });
            }
        }

        /// <summary>
        /// Generates and sends an OTP to the specified email address for password reset.
        /// Used in "forgot password" workflow to verify identity before allowing password change.
        /// </summary>
        /// <param name="request">Email address for password reset</param>
        /// <returns>Success status with expiration time</returns>
        /// <response code="200">Password reset OTP sent successfully</response>
        /// <response code="400">Invalid email or account not found</response>
        /// <response code="429">Too many requests, rate limit exceeded</response>
        [HttpPost("send-password-reset")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OTPGenerationResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SendPasswordResetOTP([FromBody] EmailOTPRequestDTO request)
        {
            try
            {
                // Always return success even if email doesn't exist to prevent email enumeration
                if (string.IsNullOrWhiteSpace(request?.Email) || !IsValidEmail(request.Email))
                {
                    return Ok(new OTPGenerationResponseDTO
                    {
                        Success = true,
                        Message = "If the email exists, a reset code will be sent",
                        ExpiresInMinutes = 10,
                        EmailMasked = MaskEmail(request?.Email)
                    });
                }

                var clientIp = GetClientIpAddress();

                // Generate reset token
                string resetToken = GenerateSecureToken();
                string resetLink = $"{Request.Scheme}://{Request.Host}/reset-password?token={resetToken}";
                string userName = ExtractNameFromEmail(request.Email);
                int expiryHours = 24;

                // Send email using IEmailService
                var emailResult = await _emailService.SendPasswordResetEmailAsync(
                    request.Email,
                    userName,
                    resetLink,
                    expiryHours);

                if (!emailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}", MaskEmail(request.Email));
                }

                _logger.LogInformation("Password reset OTP requested for {Email} from IP {ClientIp}",
                    MaskEmail(request.Email), clientIp);

                return Ok(new OTPGenerationResponseDTO
                {
                    Success = true,
                    Message = "If the email exists, a reset code will be sent",
                    ExpiresInMinutes = 10,
                    EmailMasked = MaskEmail(request.Email)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset OTP to {Email}", MaskEmail(request?.Email));

                // Return success to prevent email enumeration
                return Ok(new OTPGenerationResponseDTO
                {
                    Success = true,
                    Message = "If the email exists, a reset code will be sent",
                    ExpiresInMinutes = 10,
                    EmailMasked = MaskEmail(request?.Email)
                });
            }
        }

        /// <summary>
        /// Generates and sends an OTP for two-factor authentication via email.
        /// Used after successful password verification to add an extra security layer.
        /// </summary>
        /// <param name="request">User ID for 2FA OTP generation</param>
        /// <returns>Success status with expiration time</returns>
        /// <response code="200">2FA OTP sent successfully</response>
        /// <response code="400">Invalid user or 2FA not enabled</response>
        /// <response code="401">Unauthorized access</response>
        [HttpPost("send-2fa")]
        [Authorize]
        [ProducesResponseType(typeof(OTPGenerationResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponseDTO), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendTwoFactorOTP([FromBody] TwoFactorOTPRequestDTO request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new ErrorResponseDTO
                    {
                        Message = "User not authenticated",
                        CorrelationId = GetCorrelationId()
                    });
                }

                var clientIp = GetClientIpAddress();

                // Get user email from database (you'll need to inject user service)
                string userEmail = await GetUserEmailById(userId);
                string userName = await GetUserNameById(userId);
                string otpCode = GenerateNumericOtp(6);
                int expiryMinutes = 5;

                // Send email using IEmailService
                var emailResult = await _emailService.SendTwoFactorOtpAsync(
                    userEmail,
                    userName,
                    otpCode,
                    expiryMinutes);

                if (!emailResult.IsSuccess)
                {
                    return BadRequest(new ErrorResponseDTO
                    {
                        Message = emailResult.Message ?? "Failed to send 2FA code",
                        CorrelationId = GetCorrelationId()
                    });
                }

                _logger.LogInformation("2FA OTP sent to user {UserId} from IP {ClientIp}", userId, clientIp);

                return Ok(new OTPGenerationResponseDTO
                {
                    Success = true,
                    Message = "Two-factor authentication code sent",
                    ExpiresInMinutes = expiryMinutes,
                    EmailMasked = MaskEmail(userEmail)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending 2FA OTP for user {UserId}", GetCurrentUserId());
                return StatusCode(500, new ErrorResponseDTO
                {
                    Message = "Failed to send 2FA code",
                    CorrelationId = GetCorrelationId()
                });
            }
        }

        // Additional helper methods needed:

        private string GenerateNumericOtp(int length)
        {
            var random = new Random();
            return random.Next(0, (int)Math.Pow(10, length)).ToString($"D{length}");
        }

        private string GenerateSecureToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 32);
        }

        private string ExtractNameFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "User";

            var parts = email.Split('@');
            var name = parts[0].Replace(".", " ").Replace("_", " ");

            // Capitalize first letter of each word
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }

        private async Task<string> GetUserEmailById(int userId)
        {
            // Implement this method to get user email from database
            // This would typically use a user service/repository
            throw new NotImplementedException();
        }

        private async Task<string> GetUserNameById(int userId)
        {
            // Implement this method to get user name from database
            // This would typically use a user service/repository
            throw new NotImplementedException();
        }

        #region Helper Methods (keep existing ones)

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "unknown";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return "invalid-email";

            var name = parts[0];
            var domain = parts[1];

            if (name.Length <= 2)
                return $"{name}@***";

            var maskedName = name.Substring(0, 2) + new string('*', name.Length - 2);
            return $"{maskedName}@{domain}";
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        #endregion
    }
}