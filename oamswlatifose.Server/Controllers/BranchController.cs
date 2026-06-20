using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.DTO.Branch;
using oamswlatifose.Server.Services;
using oamswlatifose.Server.Services.Branch.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// Office/branch geofences. Any signed-in user can list them (the clock-in UI needs them);
    /// only Admin/Manager can create, update, or delete.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class BranchController : BaseApiController
    {
        private readonly IBranchService _branchService;
        private readonly ILogger<BranchController> _logger;

        public BranchController(IBranchService branchService, ILogger<BranchController> logger)
        {
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Lists branch geofences. Pass ?activeOnly=true for clock-in use.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ServiceResponse<List<BranchDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        {
            var result = await _branchService.GetAllAsync(activeOnly);
            return Ok(result);
        }

        /// <summary>Creates or updates a branch geofence (Admin/Manager).</summary>
        [HttpPost]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<BranchDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Set([FromBody] SetBranchDTO dto)
        {
            var result = await _branchService.SetAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result);

            _logger.LogInformation("Branch {Name} saved by user {UserId}", dto.Name, GetCurrentUserId());
            return Ok(result);
        }

        /// <summary>Deletes a branch geofence (Admin/Manager).</summary>
        [HttpDelete("{id:int}")]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _branchService.DeleteAsync(id);
            if (!result.IsSuccess)
                return NotFound(result);

            return Ok(result);
        }
    }
}
