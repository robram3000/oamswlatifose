using oamswlatifose.Server.Middleware;
using System.Diagnostics;
using System.Security.Claims;

namespace oamswlatifose.Server.Services
{
    /// <summary>
    /// Abstract base service class providing comprehensive infrastructure for all business services.
    /// Implements cross-cutting concerns including standardized error handling, performance monitoring,
    /// security context access, validation coordination, and audit logging.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Automatic performance monitoring with execution time tracking</para>
    /// <para>- Structured logging with correlation ID propagation</para>
    /// <para>- Current user context access and authorization helper methods</para>
    /// <para>- Standardized exception handling and response formatting</para>
    /// <para>- Validation orchestration with error aggregation</para>
    /// <para>- Distributed tracing support through correlation IDs</para>
    /// 
    /// <para>All derived services inherit these capabilities and should implement
    /// business logic through protected execution methods that leverage this infrastructure.</para>
    /// </summary>
    public abstract class BaseService
    {
        protected readonly ILogger _logger;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly ICorrelationIdGenerator _correlationIdGenerator;

        /// <summary>
        /// Initializes a new instance of the BaseService with required dependencies.
        /// </summary>
        /// <param name="logger">Logger instance for structured logging</param>
        /// <param name="httpContextAccessor">Accessor for current HTTP context and user claims</param>
        /// <param name="correlationIdGenerator">Generator for distributed tracing correlation IDs</param>
        protected BaseService(
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            ICorrelationIdGenerator correlationIdGenerator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _correlationIdGenerator = correlationIdGenerator ?? throw new ArgumentNullException(nameof(correlationIdGenerator));
        }

        /// <summary>
        /// Gets the current authenticated user's ID from the HTTP context.
        /// Returns null if user is not authenticated or ID claim is not present.
        /// </summary>
        protected int? CurrentUserId
        {
            get
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                    return userId;

                return null;
            }
        }

        /// <summary>
        /// Gets the current authenticated user's username from the HTTP context.
        /// Returns null if user is not authenticated.
        /// </summary>
        protected string CurrentUsername
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User?.Identity?.Name
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("unique_name")?.Value;
            }
        }

        /// <summary>
        /// Gets the current correlation ID for request tracing.
        /// Generates a new one if not present.
        /// </summary>
        protected string CorrelationId
        {
            get
            {
                var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();

                if (string.IsNullOrEmpty(correlationId))
                    correlationId = _correlationIdGenerator.Get();

                return correlationId;
            }
        }

        /// <summary>
        /// Gets the client IP address from the current HTTP context.
        /// </summary>
        protected string ClientIpAddress
        {
            get
            {
                return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            }
        }

        /// <summary>
        /// Gets the user agent string from the current HTTP context.
        /// </summary>
        protected string UserAgent
        {
            get
            {
                return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            }
        }

        /// <summary>
        /// Executes a function with performance tracking and standardized error handling.
        /// Automatically logs execution time and handles exceptions.
        /// </summary>
        /// <typeparam name="T">Return type of the function</typeparam>
        /// <param name="func">Async function to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <returns>ServiceResponse with execution result</returns>
        protected async Task<ServiceResponse<T>> ExecuteWithPerformanceTrackingAsync<T>(
            Func<Task<ServiceResponse<T>>> func,
            string operationName)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = CorrelationId;

            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["Operation"] = operationName,
                    ["UserId"] = CurrentUserId,
                    ["Username"] = CurrentUsername
                }))
                {
                    _logger.LogDebug("Starting operation: {OperationName}", operationName);

                    var result = await func();

                    stopwatch.Stop();
                    result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                    result.CorrelationId = correlationId;

                    _logger.LogDebug(
                        "Completed operation: {OperationName} in {ElapsedMs}ms with success: {Success}",
                        operationName,
                        stopwatch.ElapsedMilliseconds,
                        result.Success);

                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Operation {OperationName} failed after {ElapsedMs}ms: {ErrorMessage}",
                    operationName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return ServiceResponse<T>.FromException(ex, $"Operation {operationName} failed");
            }
        }

        /// <summary>
        /// Executes an action with performance tracking and standardized error handling.
        /// For void operations that don't return data.
        /// </summary>
        /// <param name="func">Async action to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <returns>ServiceResponse with execution result</returns>
        protected async Task<ServiceResponse> ExecuteWithPerformanceTrackingAsync(
            Func<Task<ServiceResponse>> func,
            string operationName)
        {
            var stopwatch = Stopwatch.StartNew();
            var correlationId = CorrelationId;

            try
            {
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId,
                    ["Operation"] = operationName,
                    ["UserId"] = CurrentUserId,
                    ["Username"] = CurrentUsername
                }))
                {
                    _logger.LogDebug("Starting operation: {OperationName}", operationName);

                    var result = await func();

                    stopwatch.Stop();
                    result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                    result.CorrelationId = correlationId;

                    _logger.LogDebug(
                        "Completed operation: {OperationName} in {ElapsedMs}ms with success: {Success}",
                        operationName,
                        stopwatch.ElapsedMilliseconds,
                        result.Success);

                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "Operation {OperationName} failed after {ElapsedMs}ms: {ErrorMessage}",
                    operationName,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                return ServiceResponse.FromException(ex, $"Operation {operationName} failed");
            }
        }

        /// <summary>
        /// Checks if the current user has a specific permission.
        /// </summary>
        /// <param name="permission">Permission name to check</param>
        /// <returns>True if user has the permission, false otherwise</returns>
        protected bool UserHasPermission(string permission)
        {
            return _httpContextAccessor.HttpContext?.User?.HasClaim("permission", permission) ?? false;
        }

        /// <summary>
        /// Checks if the current user is in a specific role.
        /// </summary>
        /// <param name="role">Role name to check</param>
        /// <returns>True if user is in the role, false otherwise</returns>
        protected bool UserIsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Validates that the current user is authorized to access a resource.
        /// Throws UnauthorizedAccessException if not authorized.
        /// </summary>
        /// <param name="resourceOwnerId">ID of the resource owner</param>
        /// <param name="requiredPermission">Required permission for access</param>
        protected void ValidateResourceAccess(int resourceOwnerId, string requiredPermission = null)
        {
            var isAdmin = UserHasPermission("admin_access");
            var isOwner = CurrentUserId == resourceOwnerId;
            var hasPermission = string.IsNullOrEmpty(requiredPermission) || UserHasPermission(requiredPermission);

            if (!isAdmin && !isOwner && !hasPermission)
            {
                _logger.LogWarning(
                    "Unauthorized access attempt: User {UserId} attempted to access resource owned by {OwnerId}",
                    CurrentUserId,
                    resourceOwnerId);

                throw new UnauthorizedAccessException("You do not have permission to access this resource");
            }
        }

        /// <summary>
        /// Creates a successful response with data.
        /// </summary>
        protected ServiceResponse<T> SuccessResponse<T>(T data, string message = null)
        {
            return ServiceResponse<T>.Success(data, message);
        }

        /// <summary>
        /// Creates a failure response with error messages.
        /// </summary>
        protected ServiceResponse<T> FailureResponse<T>(string message, IEnumerable<string> errors = null)
        {
            return ServiceResponse<T>.Failure(message, errors);
        }

        /// <summary>
        /// Creates a validation error response from FluentValidation results.
        /// </summary>
        protected ServiceResponse<T> ValidationErrorResponse<T>(FluentValidation.Results.ValidationResult validationResult)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage);
            return ServiceResponse<T>.Failure("Validation failed", errors);
        }
    }
}
