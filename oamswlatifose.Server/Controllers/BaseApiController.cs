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
            return User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("unique_name")?.Value;
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
