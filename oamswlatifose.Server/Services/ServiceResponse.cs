namespace oamswlatifose.Server.Services
{
    /// <summary>
    /// Standardized response wrapper for all service layer operations providing consistent
    /// success/failure indicators, detailed error messages, and typed data payloads.
    /// This class implements the Result Pattern to eliminate exception-based flow control
    /// and provide predictable, self-documenting service responses.
    /// 
    /// <para>Key Features:</para>
    /// <para>- Generic typed data payload for strongly-typed responses</para>
    /// <para>- Collection of validation errors for comprehensive feedback</para>
    /// <para>- Success/failure state with boolean flag</para>
    /// <para>- Human-readable messages for UI display</para>
    /// <para>- Correlation ID for distributed tracing</para>
    /// <para>- Execution time tracking for performance monitoring</para>
    /// 
    /// <para>Usage Pattern:</para>
    /// <para>var response = ServiceResponse&lt;Employee&gt;.Success(employee, "Employee retrieved successfully");</para>
    /// <para>var response = ServiceResponse&lt;Employee&gt;.Failure("Employee not found", new[] { "Invalid ID" });</para>
    /// </summary>
    /// <typeparam name="T">The type of data payload being returned by the service operation</typeparam>
    public class ServiceResponse<T>
    {
        /// <summary>
        /// Indicates whether the service operation completed successfully.
        /// True for successful operations, false for failures or validation errors.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message describing the result of the operation.
        /// Suitable for direct display in UI notifications and toast messages.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The strongly-typed data payload returned by the service operation.
        /// Null when Success is false or operation returns no data.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Collection of validation errors or detailed error messages.
        /// Used to display multiple error messages or field-specific validation feedback.
        /// </summary>
        public IEnumerable<string> Errors { get; set; }

        /// <summary>
        /// Unique correlation identifier for distributed tracing across service boundaries.
        /// Enables end-to-end request tracking in logs and monitoring systems.
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// Timestamp when the service operation was executed (UTC).
        /// Useful for caching strategies and temporal queries.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Total execution time in milliseconds for performance monitoring.
        /// Helps identify slow operations and optimization opportunities.
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Creates a successful response with data payload and optional message.
        /// </summary>
        /// <param name="data">The data payload to return</param>
        /// <param name="message">Optional success message</param>
        /// <returns>A configured success response</returns>
        public static ServiceResponse<T> Success(T data, string message = null)
        {
            return new ServiceResponse<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "Operation completed successfully",
                Errors = null,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failure response with error messages and optional details.
        /// </summary>
        /// <param name="message">Primary error message describing the failure</param>
        /// <param name="errors">Collection of detailed error messages</param>
        /// <returns>A configured failure response</returns>
        public static ServiceResponse<T> Failure(string message, IEnumerable<string> errors = null)
        {
            return new ServiceResponse<T>
            {
                Success = false,
                Data = default,
                Message = message ?? "Operation failed",
                Errors = errors ?? new[] { message },
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a failure response from an exception with detailed error information.
        /// </summary>
        /// <param name="ex">The exception that caused the failure</param>
        /// <param name="message">Optional custom error message</param>
        /// <returns>A configured failure response with exception details</returns>
        public static ServiceResponse<T> FromException(Exception ex, string message = null)
        {
            return new ServiceResponse<T>
            {
                Success = false,
                Data = default,
                Message = message ?? "An error occurred while processing your request",
                Errors = new[]
                {
                    ex.Message,
                    ex.InnerException?.Message
                }.Where(e => e != null),
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Non-generic version of ServiceResponse for operations that don't return data.
    /// Provides consistent response pattern for void operations like delete, update, etc.
    /// </summary>
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public string CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public long ExecutionTimeMs { get; set; }

        public static ServiceResponse Success(string message = null)
        {
            return new ServiceResponse
            {
                Success = true,
                Message = message ?? "Operation completed successfully",
                Errors = null,
                Timestamp = DateTime.UtcNow
            };
        }

        public static ServiceResponse Failure(string message, IEnumerable<string> errors = null)
        {
            return new ServiceResponse
            {
                Success = false,
                Message = message ?? "Operation failed",
                Errors = errors ?? new[] { message },
                Timestamp = DateTime.UtcNow
            };
        }

        public static ServiceResponse FromException(Exception ex, string message = null)
        {
            return new ServiceResponse
            {
                Success = false,
                Message = message ?? "An error occurred while processing your request",
                Errors = new[]
                {
                    ex.Message,
                    ex.InnerException?.Message
                }.Where(e => e != null),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
