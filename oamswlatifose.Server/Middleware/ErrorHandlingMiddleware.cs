using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace oamswlatifose.Server.Middleware
{
    /// <summary>
    /// Global error handling middleware for consistent exception handling.
    /// Captures unhandled exceptions and returns standardized error responses.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                                context.TraceIdentifier;

            var (statusCode, message, detailedErrors) = GetExceptionResponse(ex);

            response.StatusCode = statusCode;

            object errorResponse;

            if (_env.IsDevelopment())
            {
                var devErrors = new List<string>(detailedErrors ?? Array.Empty<string>())
                {
                    $"Exception Type: {ex.GetType().Name}",
                    $"Stack Trace: {ex.StackTrace}"
                };
                if (ex.InnerException != null)
                {
                    devErrors.Add($"Inner Exception: {ex.InnerException.Message}");
                }

                errorResponse = new
                {
                    Success = false,
                    Message = message,
                    Errors = devErrors, // This is now List<string> instead of string[]
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    Path = context.Request.Path,
                    Method = context.Request.Method
                };
            }
            else
            {
                errorResponse = new
                {
                    Success = false,
                    Message = message,
                    Errors = detailedErrors,
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow,
                    Path = context.Request.Path,
                    Method = context.Request.Method
                };
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }

        private (int statusCode, string message, string[] errors) GetExceptionResponse(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException => (401, "You are not authorized to access this resource",
                    new[] { "Authentication required or insufficient permissions" }),

                KeyNotFoundException => (404, "The requested resource was not found",
                    new[] { ex.Message }),

                ArgumentException => (400, "Invalid request parameters",
                    new[] { ex.Message }),

                InvalidOperationException => (400, "The operation could not be completed",
                    new[] { ex.Message }),

                // Order matters - put more specific exceptions first
                DbUpdateConcurrencyException => (409, "The data was modified by another user",
                    new[] { "Please refresh and try again" }),

                DbUpdateException => (500, "A database error occurred while processing your request",
                    new[] { _env.IsDevelopment() ? ex.Message : "Please try again later" }),

                _ => (500, "An unexpected error occurred while processing your request",
                    new[] { _env.IsDevelopment() ? ex.Message : "Please contact support if the issue persists" })
            };
        }
    }
}