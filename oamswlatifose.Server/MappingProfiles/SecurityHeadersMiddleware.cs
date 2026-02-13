namespace oamswlatifose.Server.MappingProfiles
{
    /// <summary>
    /// Middleware for adding security headers to all HTTP responses.
    /// Implements security best practices to protect against common web vulnerabilities.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            AddSecurityHeaders(context.Response);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpResponse response)
        {
            // Prevent MIME type sniffing
            response.Headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            response.Headers["X-Frame-Options"] = "DENY";

            // Enable XSS protection
            response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Strict Transport Security (HSTS)
            response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload";

            // Content Security Policy
            response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self'";

            // Referrer Policy
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions Policy
            response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(self), payment=()";

            // Remove Server header
            response.Headers.Remove("Server");

            // Remove X-Powered-By header
            response.Headers.Remove("X-Powered-By");

            // Remove X-AspNet-Version header
            response.Headers.Remove("X-AspNet-Version");
        }
    }
}
