using System.Collections.Concurrent;

namespace oamswlatifose.Server.Middleware
{
    /// <summary>
    /// Middleware for API rate limiting to prevent abuse and ensure fair usage.
    /// Implements sliding window algorithm per client IP address.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, ClientRequestTracker> _clientTrackers = new();
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _maxRequests = configuration.GetValue<int>("RateLimiting:MaxRequests", 100);
            _timeWindow = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:TimeWindowSeconds", 60));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsExcludedPath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);

            try
            {
                await _semaphore.WaitAsync();

                var tracker = _clientTrackers.GetOrAdd(clientId, _ => new ClientRequestTracker());

                if (!tracker.IsRequestAllowed(_maxRequests, _timeWindow))
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);

                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
                    context.Response.Headers["X-RateLimit-Remaining"] = "0";
                    context.Response.Headers["X-RateLimit-Reset"] =
                        tracker.GetResetTime(_timeWindow).ToUnixTimeSeconds().ToString();
                    context.Response.Headers["Retry-After"] =
                        tracker.GetTimeUntilReset(_timeWindow).Seconds.ToString();

                    await context.Response.WriteAsJsonAsync(new
                    {
                        Success = false,
                        Message = "Too many requests. Please try again later.",
                        RetryAfterSeconds = tracker.GetTimeUntilReset(_timeWindow).Seconds
                    });

                    return;
                }

                tracker.IncrementRequestCount();

                context.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] =
                    (_maxRequests - tracker.RequestCount).ToString();
                context.Response.Headers["X-RateLimit-Reset"] =
                    tracker.GetResetTime(_timeWindow).ToUnixTimeSeconds().ToString();

                await _next(context);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool IsExcludedPath(PathString path)
        {
            var excludedPaths = new[]
            {
                "/health",
                "/metrics",
                "/swagger",
                "/favicon.ico"
            };

            return excludedPaths.Any(p => path.StartsWithSegments(p));
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Use authenticated user ID if available, otherwise IP address
            var userId = context.User?.FindFirst("user_id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user_{userId}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip_{ipAddress}";
        }

        private class ClientRequestTracker
        {
            private readonly Queue<DateTime> _requestTimestamps = new();
            private readonly object _lock = new();

            public int RequestCount => _requestTimestamps.Count;

            public bool IsRequestAllowed(int maxRequests, TimeSpan timeWindow)
            {
                lock (_lock)
                {
                    var now = DateTime.UtcNow;

                    // Remove timestamps outside the current window
                    while (_requestTimestamps.Count > 0 &&
                           now - _requestTimestamps.Peek() > timeWindow)
                    {
                        _requestTimestamps.Dequeue();
                    }

                    return _requestTimestamps.Count < maxRequests;
                }
            }

            public void IncrementRequestCount()
            {
                lock (_lock)
                {
                    _requestTimestamps.Enqueue(DateTime.UtcNow);
                }
            }

            public DateTimeOffset GetResetTime(TimeSpan timeWindow)
            {
                lock (_lock)
                {
                    if (_requestTimestamps.Count == 0)
                        return DateTimeOffset.UtcNow;

                    var oldestRequest = _requestTimestamps.Peek();
                    return new DateTimeOffset(oldestRequest.Add(timeWindow));
                }
            }

            public TimeSpan GetTimeUntilReset(TimeSpan timeWindow)
            {
                var resetTime = GetResetTime(timeWindow);
                var now = DateTimeOffset.UtcNow;

                return resetTime > now ? resetTime - now : TimeSpan.Zero;
            }
        }
    }
}