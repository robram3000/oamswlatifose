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
    }
}
