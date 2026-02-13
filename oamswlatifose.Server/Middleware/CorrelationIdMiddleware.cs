using Microsoft.Extensions.Primitives;

namespace oamswlatifose.Server.Middleware
{
    /// <summary>
    /// Middleware for generating and propagating correlation IDs across requests.
    /// Enables distributed tracing and request correlation in logs.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId;

            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out StringValues correlationIdValues))
            {
                correlationId = correlationIdValues.First();
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers.Append(CorrelationIdHeader, correlationId);
            }

            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            context.TraceIdentifier = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Interface for correlation ID generation and access.
    /// </summary>
    public interface ICorrelationIdGenerator
    {
        string Generate();
        string Get();
    }

    /// <summary>
    /// Implementation of correlation ID generator.
    /// </summary>
    public class CorrelationIdGenerator : ICorrelationIdGenerator
    {
        private string _correlationId;

        public string Generate() => _correlationId = Guid.NewGuid().ToString();

        public string Get() => _correlationId ??= Guid.NewGuid().ToString();
    }
}
