using System.Diagnostics;
using System.Text;

namespace oamswlatifose.Server.Middleware
{
    /// <summary>
    /// Middleware for logging HTTP requests and responses with performance metrics.
    /// Provides detailed logging for monitoring, debugging, and security auditing.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestBody = await ReadRequestBody(context.Request);
            var originalResponseBody = context.Response.Body;

            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                var responseContent = await ReadResponseBody(context.Response);
                await LogRequestDetails(context, requestBody, responseContent, stopwatch.ElapsedMilliseconds);

                await responseBody.CopyToAsync(originalResponseBody);
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }

        private async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return null;

            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            return body;
        }

        private async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return body;
        }

        private async Task LogRequestDetails(
            HttpContext context,
            string requestBody,
            string responseBody,
            long elapsedMs)
        {
            var request = context.Request;
            var response = context.Response;

            var logData = new
            {
                Timestamp = DateTime.UtcNow,
                RequestId = context.TraceIdentifier,
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                StatusCode = response.StatusCode,
                ElapsedMs = elapsedMs,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = request.Headers["User-Agent"].ToString(),
                UserId = context.User?.FindFirst("user_id")?.Value,
                Username = context.User?.Identity?.Name,
                RequestSize = requestBody?.Length ?? 0,
                ResponseSize = responseBody?.Length ?? 0
            };

            var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                          response.StatusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            if (logLevel == LogLevel.Error || logLevel == LogLevel.Warning || _logger.IsEnabled(LogLevel.Debug))
            {
                var logMessage = $"HTTP {request.Method} {request.Path} responded {response.StatusCode} in {elapsedMs}ms";

                using (_logger.BeginScope(logData))
                {
                    _logger.Log(logLevel, logMessage);

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        if (!string.IsNullOrEmpty(requestBody))
                        {
                            _logger.LogDebug("Request Body: {RequestBody}",
                                TruncateLogContent(requestBody, 1000));
                        }

                        if (!string.IsNullOrEmpty(responseBody) &&
                            response.ContentType?.Contains("application/json") == true)
                        {
                            _logger.LogDebug("Response Body: {ResponseBody}",
                                TruncateLogContent(responseBody, 1000));
                        }
                    }
                }
            }
        }

        private string TruncateLogContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
                return content;

            return content.Substring(0, maxLength) + "... (truncated)";
        }
    }
}
