using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.DTO.Schedule;
using oamswlatifose.Server.Services;
using oamswlatifose.Server.Services.Schedule.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// Work-schedule management. The scheduled start time + grace window is what attendance
    /// is graded against, so this is the "set the schedule time" side of the feature.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ScheduleController : BaseApiController
    {
        private readonly IWorkScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(IWorkScheduleService scheduleService, ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Gets the logged-in employee's own active schedule.</summary>
        [HttpGet("my")]
        [ProducesResponseType(typeof(ServiceResponse<WorkScheduleDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySchedule()
        {
            var employeeId = GetCurrentEmployeeId();
            if (employeeId == 0)
                return Ok(ServiceResponse<WorkScheduleDTO>.FailureResult("No employee record linked to your account"));

            var result = await _scheduleService.GetByEmployeeAsync(employeeId);
            return Ok(result);
        }

        /// <summary>Lists every employee's active schedule (Admin/Manager).</summary>
        [HttpGet]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<List<WorkScheduleDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _scheduleService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>Gets a specific employee's schedule (Admin/Manager).</summary>
        [HttpGet("employee/{employeeId:int}")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<WorkScheduleDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByEmployee(int employeeId)
        {
            var result = await _scheduleService.GetByEmployeeAsync(employeeId);
            return Ok(result);
        }

        /// <summary>Removes (deactivates) a specific employee's schedule (Admin/Manager).</summary>
        [HttpDelete("employee/{employeeId:int}")]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(int employeeId)
        {
            var result = await _scheduleService.DeleteAsync(employeeId);
            if (!result.IsSuccess)
                return BadRequest(result);
            _logger.LogInformation("Schedule deleted for employee {EmployeeId} by user {UserId}",
                employeeId, GetCurrentUserId());
            return Ok(result);
        }

        /// <summary>Creates or updates an employee's schedule (Admin/Manager).</summary>
        [HttpPost]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<WorkScheduleDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Set([FromBody] SetWorkScheduleDTO dto)
        {
            var result = await _scheduleService.SetAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(result);

            _logger.LogInformation("Schedule saved for employee {EmployeeId} by user {UserId}",
                dto.EmployeeId, GetCurrentUserId());
            return Ok(result);
        }
    }
}
