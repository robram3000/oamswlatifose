using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.Controllers;
using oamswlatifose.Server.DTO.User;
using System.Security.Claims;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// API controller for authentication and authorization operations.
    /// Provides endpoints for login, registration, token refresh, and password management.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user and returns access tokens.
        /// </summary>
        /// <param name="loginRequest">Login credentials</param>
        /// <returns>Access token, refresh token, and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResponse<LoginResponseDTO>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequest)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.LoginAsync(loginRequest, ipAddress, userAgent);

            if (!result.Success)
            {
                if (result.Message.Contains("Invalid") || result.Message.Contains("deactivated"))
                    return Unauthorized(result);

                return BadRequest(result);
            }

            SetTokenCookies(result.Data.AccessToken, result.Data.RefreshToken, result.Data.ExpiresAt);

            return Ok(result);
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="registerRequest">Registration data</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<UserResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceResponse<UserResponseDTO>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequest)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.RegisterAsync(registerRequest, ipAddress, userAgent);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetCurrentUser), new { }, result);
        }

        /// <summary>
        /// Refreshes an expired access token.
        /// </summary>
        /// <param name="refreshRequest">Refresh token</param>
        /// <returns>New access and refresh tokens</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<RefreshTokenResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<RefreshTokenResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResponse<RefreshTokenResponseDTO>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO refreshRequest)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.RefreshTokenAsync(refreshRequest.RefreshToken, ipAddress, userAgent);

            if (!result.Success)
            {
                if (result.Message.Contains("Invalid"))
                    return Unauthorized(result);

                return BadRequest(result);
            }

            SetTokenCookies(result.Data.AccessToken, result.Data.RefreshToken, result.Data.ExpiresAt);

            return Ok(result);
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        /// <returns>Logout result</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            var userId = GetCurrentUserId();
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            var result = await _authService.LogoutAsync(userId, accessToken);

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return Ok(result);
        }

        /// <summary>
        /// Changes the current user's password.
        /// </summary>
        /// <param name="changePasswordDto">Password change data</param>
        /// <returns>Change result</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePasswordDto)
        {
            var userId = GetCurrentUserId();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto, ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Initiates password reset process.
        /// </summary>
        /// <param name="forgotPasswordDto">Forgot password request</param>
        /// <returns>Reset instructions sent status</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO forgotPasswordDto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var result = await _authService.ForgotPasswordAsync(forgotPasswordDto, ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Resets password using reset token.
        /// </summary>
        /// <param name="resetPasswordDto">Reset password data</param>
        /// <returns>Reset result</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

            var result = await _authService.ResetPasswordAsync(resetPasswordDto, ipAddress);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Verifies user email address.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="token">Verification token</param>
        /// <returns>Verification result</returns>
        [HttpGet("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromQuery] int userId, [FromQuery] string token)
        {
            var result = await _authService.VerifyEmailAsync(userId, token);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Gets the currently authenticated user.
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ServiceResponse<UserResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<UserResponseDTO>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ServiceResponse<UserResponseDTO>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();
            var result = await _authService.GetCurrentUserAsync(userId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        private void SetTokenCookies(string accessToken, string refreshToken, DateTime expiresAt)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt
            };

            Response.Cookies.Append("access_token", accessToken, cookieOptions);

            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refresh_token", refreshToken, refreshCookieOptions);
        }
    }
}
File: BaseApiController.cs

csharp
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// Base API controller providing common functionality for all controllers.
    /// Includes helper methods for user context, correlation IDs, and standardized responses.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Gets the current authenticated user's ID from claims.
        /// </summary>
        /// <returns>User ID if authenticated; otherwise, 0</returns>
        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("user_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return 0;
        }

        /// <summary>
        /// Gets the current authenticated user's username from claims.
        /// </summary>
        /// <returns>Username if authenticated; otherwise, null</returns>
        protected string GetCurrentUsername()
        {
            return User.FindFirst(ClaimTypes.Name) ?? User.FindFirst("unique_name")?.Value;
        }

        /// <summary>
        /// Checks if the current user has a specific permission.
        /// </summary>
        /// <param name="permission">Permission name to check</param>
        /// <returns>True if user has permission; otherwise, false</returns>
        protected bool UserHasPermission(string permission)
        {
            return User.HasClaim("permission", permission);
        }

        /// <summary>
        /// Gets the correlation ID for the current request.
        /// </summary>
        protected string GetCorrelationId()
        {
            if (Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
                return correlationId.ToString();

            return HttpContext.TraceIdentifier;
        }

        /// <summary>
        /// Creates a bad request response with validation errors.
        /// </summary>
        protected IActionResult ValidationError(string message, IEnumerable<string> errors)
        {
            return BadRequest(new
            {
                Success = false,
                Message = message,
                Errors = errors,
                Timestamp = DateTime.UtcNow,
                CorrelationId = GetCorrelationId()
            });
        }
    }
}
