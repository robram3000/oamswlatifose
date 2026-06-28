using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.Licensing;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// Exposes license status and activation for this deployment.
    /// These endpoints are always accessible (bypass the license middleware) so that
    /// operators can check and activate a license even after the trial expires.
    ///
    /// <para>License: Proprietary software by Roberto V Ramirez Jr (robram3000@gmail.com).
    /// Trial period is 30 days from first deployment. On day 31 a valid license key
    /// issued by robram3000@gmail.com must be activated via POST /api/license/activate.</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;

        public LicenseController(ILicenseService licenseService) => _licenseService = licenseService;

        /// <summary>Returns the current license/trial status of this deployment.</summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var state = _licenseService.GetCurrentState();
            return Ok(new
            {
                status = state.Status.ToString(),
                firstRunDate = state.FirstRunDate,
                expiryDate = state.ExpiryDate,
                licensedTo = state.LicensedTo,
                trialDaysRemaining = state.TrialDaysRemaining,
                contact = "robram3000@gmail.com",
                message = state.Status switch
                {
                    LicenseStatus.Trial =>
                        $"Trial — {state.TrialDaysRemaining} day(s) remaining. Contact robram3000@gmail.com to obtain a license.",
                    LicenseStatus.Licensed =>
                        $"Licensed to {state.LicensedTo}. Valid until {state.ExpiryDate:yyyy-MM-dd}.",
                    LicenseStatus.Expired =>
                        "License or trial has expired. Contact robram3000@gmail.com for a license key.",
                    _ =>
                        "No valid license found. Contact robram3000@gmail.com."
                }
            });
        }

        /// <summary>Activates a license key issued by robram3000@gmail.com.</summary>
        [HttpPost("activate")]
        public IActionResult Activate([FromBody] ActivateLicenseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.LicenseKey))
                return BadRequest(new { success = false, message = "License key is required." });

            var (success, message) = _licenseService.ActivateLicense(request.LicenseKey.Trim());
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        /// <summary>
        /// Requests a duration-based license (1month / 1year / 2year). Emails robram3000@gmail.com
        /// a confirmation link; access begins once the owner confirms it.
        /// </summary>
        [HttpPost("request")]
        public async Task<IActionResult> RequestLicense([FromBody] RequestLicenseRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.DurationKey))
                return BadRequest(new { success = false, message = "A license duration is required." });

            // Build the confirm link from the incoming request so it works on any host.
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var requester = request.Requester
                ?? User?.Identity?.Name
                ?? Request.Host.Value;

            var (success, message) = await _licenseService.RequestLicenseAsync(request.DurationKey.Trim(), requester, baseUrl);
            return success ? Ok(new { success = true, message }) : BadRequest(new { success = false, message });
        }

        /// <summary>
        /// Owner confirmation endpoint reached from the email link. Activates the requested duration
        /// and returns a simple HTML page (this is opened in a browser, not by the SPA).
        /// </summary>
        [HttpGet("confirm")]
        public IActionResult Confirm([FromQuery] string id, [FromQuery] string token)
        {
            var (success, message, _) = _licenseService.ConfirmLicense(id, token);
            var color = success ? "#188038" : "#d93025";
            var heading = success ? "License activated" : "Activation failed";
            var html = $@"<!doctype html><html><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>{heading}</title></head>
<body style='font-family:Arial,sans-serif;background:#f8f9fa;margin:0;padding:40px'>
  <div style='max-width:460px;margin:auto;background:#fff;border:1px solid #e0e0e0;border-radius:10px;padding:32px;text-align:center'>
    <div style='font-size:40px'>{(success ? "✅" : "⚠️")}</div>
    <h2 style='color:{color};margin:12px 0 8px'>{heading}</h2>
    <p style='color:#5f6368;font-size:14px;line-height:1.5'>{System.Net.WebUtility.HtmlEncode(message)}</p>
  </div>
</body></html>";
            return Content(html, "text/html");
        }
    }
}
