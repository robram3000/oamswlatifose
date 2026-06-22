using oamswlatifose.Server.Licensing;
using System.Text.Json;

namespace oamswlatifose.Server.Middleware
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        // These path prefixes bypass the license check so users can still log in
        // and activate a license even when the trial has expired.
        private static readonly string[] BypassPrefixes =
        [
            "/api/license",
            "/api/auth/login",
            "/api/auth/refresh",
            "/health",
            "/swagger",
        ];

        public LicenseMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isApiCall = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

            if (!isApiCall || BypassPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            if (!licenseService.IsAccessAllowed())
            {
                var state = licenseService.GetCurrentState();
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                context.Response.ContentType = "application/json";

                var body = new
                {
                    success = false,
                    message = "This deployment requires a valid license to operate.",
                    licenseStatus = state.Status.ToString(),
                    expiryDate = state.ExpiryDate,
                    contact = "robram3000@gmail.com",
                    activationEndpoint = "/api/license/activate"
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                return;
            }

            await _next(context);
        }
    }
}
