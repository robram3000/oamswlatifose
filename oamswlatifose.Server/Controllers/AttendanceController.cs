using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using oamswlatifose.Server.DTO.attendances;
using oamswlatifose.Server.Services;
using oamswlatifose.Server.Services.Attendance.Interfaces;
using oamswlatifose.Server.Utilities.Security;

namespace oamswlatifose.Server.Controllers
{
    /// <summary>
    /// API controller for employee attendance management operations.
    /// Provides comprehensive endpoints for clock in/out, attendance tracking,
    /// reporting, and administrative management of attendance records.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Employee clock-in and clock-out operations</para>
    /// <para>- Daily, weekly, monthly attendance retrieval</para>
    /// <para>- Attendance report generation with filters</para>
    /// <para>- Overtime calculation and tracking</para>
    /// <para>- Late arrival monitoring</para>
    /// <para>- Absence tracking and reporting</para>
    /// <para>- Bulk attendance operations for admins</para>
    /// <para>- Export attendance data to various formats</para>
    /// 
    /// <para>Security:</para>
    /// <para>- Employees can only access their own attendance</para>
    /// <para>- Managers can access department attendance</para>
    /// <para>- Admins have full access to all records</para>
    /// <para>- Geolocation validation for clock-in/out</para>
    /// <para>- IP tracking for security auditing</para>
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AttendanceController : BaseApiController
    {
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            IAttendanceService attendanceService,
            ILogger<AttendanceController> logger)
        {
            _attendanceService = attendanceService ?? throw new ArgumentNullException(nameof(attendanceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Employee Self-Service Endpoints

        /// <summary>
        /// Records employee clock-in for the current day.
        /// Automatically sets attendance date to today and records the time.
        /// Prevents duplicate clock-ins for the same day.
        /// </summary>
        /// <param name="clockInDto">Clock-in data with optional geolocation</param>
        /// <returns>Created attendance record</returns>
        /// <response code="200">Clock-in successful</response>
        /// <response code="400">Already clocked in today or invalid data</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("clock-in")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ClockIn([FromBody] ClockInDTO clockInDto)
        {
            try
            {
                var employeeId = GetEmployeeIdForCurrentUser();
                if (employeeId == 0)
                {
                    return BadRequest(ServiceResponse<AttendanceResponseDTO>.FailureResult(
                        "No employee record linked to your account"));
                }

                clockInDto.EmployeeId = employeeId;
                clockInDto.DeviceInfo = GetDeviceInfo();

                var clientIp = GetClientIpAddress();
                var result = await _attendanceService.ClockInAsync(clockInDto, clientIp);

                if (!result.IsSuccess)
                    return BadRequest(result);

                _logger.LogInformation("Employee {EmployeeId} clocked in at {Time}",
                    employeeId, result.Data?.TimeInFormatted);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-in for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to clock in"));
            }
        }

        /// <summary>
        /// Records employee clock-out for the current day.
        /// Updates the existing attendance record with clock-out time and calculates hours worked.
        /// </summary>
        /// <param name="clockOutDto">Clock-out data with optional geolocation</param>
        /// <returns>Updated attendance record with calculated hours</returns>
        /// <response code="200">Clock-out successful</response>
        /// <response code="400">No active clock-in found or invalid data</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("clock-out")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ClockOut([FromBody] ClockOutDTO clockOutDto)
        {
            try
            {
                var employeeId = GetEmployeeIdForCurrentUser();
                if (employeeId == 0)
                {
                    return BadRequest(ServiceResponse<AttendanceResponseDTO>.FailureResult(
                        "No employee record linked to your account"));
                }

                clockOutDto.EmployeeId = employeeId;

                var clientIp = GetClientIpAddress();
                var result = await _attendanceService.ClockOutAsync(clockOutDto, clientIp);

                if (!result.IsSuccess)
                    return BadRequest(result);

                _logger.LogInformation("Employee {EmployeeId} clocked out at {Time}, Hours worked: {Hours}",
                    employeeId, result.Data?.TimeOutFormatted, result.Data?.HoursWorked);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during clock-out for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to clock out"));
            }
        }

        /// <summary>
        /// Gets the current day's attendance status for the logged-in employee.
        /// Shows whether clocked in, clocked out, or not started.
        /// </summary>
        /// <returns>Today's attendance status</returns>
        [HttpGet("my-today")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyTodayAttendance()
        {
            try
            {
                var employeeId = GetEmployeeIdForCurrentUser();
                if (employeeId == 0)
                {
                    return Ok(ServiceResponse<AttendanceResponseDTO>.SuccessResult(
                        null, "No attendance record for today"));
                }

                var result = await _attendanceService.GetTodayAttendanceAsync(employeeId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's attendance for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to get attendance status"));
            }
        }

        /// <summary>
        /// Gets paginated attendance history for the logged-in employee.
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paginated attendance history</returns>
        [HttpGet("my-history")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<AttendanceSummaryDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyAttendanceHistory(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var employeeId = GetEmployeeIdForCurrentUser();
                if (employeeId == 0)
                {
                    return Ok(ServiceResponse<PagedResult<AttendanceSummaryDTO>>.SuccessResult(
                        new PagedResult<AttendanceSummaryDTO>(), "No attendance records found"));
                }

                pageSize = Math.Min(pageSize, 100);
                var result = await _attendanceService.GetEmployeeAttendanceHistoryAsync(
                    employeeId, pageNumber, pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance history for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ServiceResponse<PagedResult<AttendanceSummaryDTO>>.FromException(
                    ex, "Failed to get attendance history"));
            }
        }

        /// <summary>
        /// Gets attendance summary for the logged-in employee within a date range.
        /// Includes total hours, overtime, and days present.
        /// </summary>
        /// <param name="startDate">Start date (YYYY-MM-DD)</param>
        /// <param name="endDate">End date (YYYY-MM-DD)</param>
        /// <returns>Attendance summary statistics</returns>
        [HttpGet("my-summary")]
        [ProducesResponseType(typeof(ServiceResponse<EmployeeAttendanceSummaryDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyAttendanceSummary(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                var employeeId = GetEmployeeIdForCurrentUser();
                if (employeeId == 0)
                {
                    return BadRequest(ServiceResponse<EmployeeAttendanceSummaryDTO>.FailureResult(
                        "No employee record linked to your account"));
                }

                if (endDate < startDate)
                {
                    return BadRequest(ServiceResponse<EmployeeAttendanceSummaryDTO>.FailureResult(
                        "End date must be after start date"));
                }

                // Limit date range to 3 months for performance
                if ((endDate - startDate).TotalDays > 90)
                {
                    return BadRequest(ServiceResponse<EmployeeAttendanceSummaryDTO>.FailureResult(
                        "Date range cannot exceed 90 days"));
                }

                var result = await _attendanceService.GetEmployeeAttendanceSummaryAsync(
                    employeeId, startDate, endDate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance summary for user {UserId}", GetCurrentUserId());
                return StatusCode(500, ServiceResponse<EmployeeAttendanceSummaryDTO>.FromException(
                    ex, "Failed to get attendance summary"));
            }
        }

        #endregion

        #region Manager/Admin Endpoints

        /// <summary>
        /// Gets paginated attendance records for all employees (Admin/Manager only).
        /// Supports filtering by date, department, and status.
        /// </summary>
        /// <param name="filter">Attendance filter parameters</param>
        /// <returns>Paginated attendance records</returns>
        [HttpGet("admin/all")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<AttendanceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllAttendance([FromQuery] AttendanceFilterDTO filter)
        {
            try
            {
                filter.PageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
                filter.PageSize = Math.Min(filter.PageSize, 100);

                var result = await _attendanceService.GetAllAttendanceAsync(filter);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all attendance records");
                return StatusCode(500, ServiceResponse<PagedResult<AttendanceResponseDTO>>.FromException(
                    ex, "Failed to get attendance records"));
            }
        }

        /// <summary>
        /// Gets attendance records for a specific employee (Manager/Admin only).
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated employee attendance</returns>
        [HttpGet("admin/employee/{employeeId}")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<PagedResult<AttendanceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmployeeAttendance(
            int employeeId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                pageSize = Math.Min(pageSize, 100);
                var result = await _attendanceService.GetEmployeeAttendanceHistoryAsync(
                    employeeId, pageNumber, pageSize);

                if (!result.IsSuccess && result.Message.Contains("not found"))
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for employee {EmployeeId}", employeeId);
                return StatusCode(500, ServiceResponse<PagedResult<AttendanceResponseDTO>>.FromException(
                    ex, "Failed to get employee attendance"));
            }
        }

        /// <summary>
        /// Gets attendance records by date (Admin/Manager only).
        /// Shows all employees' attendance for a specific date.
        /// </summary>
        /// <param name="date">Date (YYYY-MM-DD)</param>
        /// <returns>Attendance records for the date</returns>
        [HttpGet("admin/by-date/{date}")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<AttendanceResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAttendanceByDate(DateTime date)
        {
            try
            {
                var result = await _attendanceService.GetAttendanceByDateAsync(date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance for date {Date}", date);
                return StatusCode(500, ServiceResponse<IEnumerable<AttendanceResponseDTO>>.FromException(
                    ex, "Failed to get attendance records"));
            }
        }

        /// <summary>
        /// Gets attendance report for a date range with department filtering (Admin/Manager only).
        /// </summary>
        /// <param name="request">Report request parameters</param>
        /// <returns>Attendance report data</returns>
        [HttpPost("admin/report")]
        [PermissionAuthorize("generate_reports")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceReportDataDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateAttendanceReport([FromBody] AttendanceReportRequestDTO request)
        {
            try
            {
                if (request.EndDate < request.StartDate)
                {
                    return BadRequest(ServiceResponse<AttendanceReportDataDTO>.FailureResult(
                        "End date must be after start date"));
                }

                var result = await _attendanceService.GenerateAttendanceReportAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating attendance report");
                return StatusCode(500, ServiceResponse<AttendanceReportDataDTO>.FromException(
                    ex, "Failed to generate report"));
            }
        }

        /// <summary>
        /// Gets late arrivals for a specific date (Admin/Manager only).
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <param name="threshold">Late threshold time (default: 09:15)</param>
        /// <returns>List of late arrivals</returns>
        [HttpGet("admin/late-arrivals")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<LateArrivalDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLateArrivals(
            [FromQuery] DateTime date,
            [FromQuery] string threshold = "09:15")
        {
            try
            {
                if (!TimeSpan.TryParse(threshold, out var lateThreshold))
                {
                    lateThreshold = new TimeSpan(9, 15, 0);
                }

                var result = await _attendanceService.GetLateArrivalsAsync(date, lateThreshold);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting late arrivals for {Date}", date);
                return StatusCode(500, ServiceResponse<IEnumerable<LateArrivalDTO>>.FromException(
                    ex, "Failed to get late arrivals"));
            }
        }

        /// <summary>
        /// Gets absent employees for a specific date (Admin/Manager only).
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>List of absent employees</returns>
        [HttpGet("admin/absent-employees")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<IEnumerable<AbsentEmployeeDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAbsentEmployees([FromQuery] DateTime date)
        {
            try
            {
                var result = await _attendanceService.GetAbsentEmployeesAsync(date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting absent employees for {Date}", date);
                return StatusCode(500, ServiceResponse<IEnumerable<AbsentEmployeeDTO>>.FromException(
                    ex, "Failed to get absent employees"));
            }
        }

        /// <summary>
        /// Gets department attendance statistics (Admin/Manager only).
        /// </summary>
        /// <param name="department">Department name</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Department statistics</returns>
        [HttpGet("admin/department-stats")]
        [PermissionAuthorize("generate_reports")]
        [ProducesResponseType(typeof(ServiceResponse<DepartmentAttendanceStatsDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentStatistics(
            [FromQuery] string department,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(department))
                {
                    return BadRequest(ServiceResponse<DepartmentAttendanceStatsDTO>.FailureResult(
                        "Department is required"));
                }

                var result = await _attendanceService.GetDepartmentStatisticsAsync(
                    department, startDate, endDate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting department stats for {Department}", department);
                return StatusCode(500, ServiceResponse<DepartmentAttendanceStatsDTO>.FromException(
                    ex, "Failed to get department statistics"));
            }
        }

        /// <summary>
        /// Exports attendance data to specified format (Admin/Manager only).
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>Exported file</returns>
        [HttpPost("admin/export")]
        [PermissionAuthorize("generate_reports")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportAttendance([FromBody] AttendanceExportRequestDTO request)
        {
            try
            {
                var result = await _attendanceService.ExportAttendanceAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                var contentType = request.Format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "pdf" => "application/pdf",
                    _ => "application/json"
                };

                var fileName = $"attendance_export_{DateTime.Now:yyyyMMdd_HHmmss}.{request.Format}";

                return File(result.Data, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance data");
                return StatusCode(500, ServiceResponse<byte[]>.FromException(
                    ex, "Failed to export attendance data"));
            }
        }

        #endregion

        #region Admin-Only CRUD Operations

        /// <summary>
        /// Creates a manual attendance record (Admin only).
        /// Used for correcting missed clock-ins or historical data entry.
        /// </summary>
        /// <param name="createDto">Attendance creation data</param>
        /// <returns>Created attendance record</returns>
        [HttpPost("admin")]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateAttendance([FromBody] CreateAttendanceDTO createDto)
        {
            try
            {
                var result = await _attendanceService.CreateAttendanceAsync(createDto, GetCurrentUserId());

                if (!result.IsSuccess)
                    return BadRequest(result);

                _logger.LogInformation("Attendance record created by admin {AdminId} for employee {EmployeeId}",
                    GetCurrentUserId(), createDto.EmployeeId);

                return CreatedAtAction(nameof(GetAttendanceById), new { id = result.Data.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating attendance record");
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to create attendance record"));
            }
        }

        /// <summary>
        /// Gets a specific attendance record by ID (Admin only).
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <returns>Attendance record</returns>
        [HttpGet("admin/{id}")]
        [PermissionAuthorize("view_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAttendanceById(int id)
        {
            try
            {
                var result = await _attendanceService.GetAttendanceByIdAsync(id);

                if (!result.IsSuccess)
                    return NotFound(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendance record {Id}", id);
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to get attendance record"));
            }
        }

        /// <summary>
        /// Updates an existing attendance record (Admin only).
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <param name="updateDto">Update data</param>
        /// <returns>Updated attendance record</returns>
        [HttpPut("admin/{id}")]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<AttendanceResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateAttendance(int id, [FromBody] UpdateAttendanceDTO updateDto)
        {
            try
            {
                var result = await _attendanceService.UpdateAttendanceAsync(id, updateDto, GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);

                    return BadRequest(result);
                }

                _logger.LogInformation("Attendance record {Id} updated by admin {AdminId}",
                    id, GetCurrentUserId());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attendance record {Id}", id);
                return StatusCode(500, ServiceResponse<AttendanceResponseDTO>.FromException(
                    ex, "Failed to update attendance record"));
            }
        }

        /// <summary>
        /// Deletes an attendance record (Admin only).
        /// Use with caution - this operation is irreversible.
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("admin/{id}")]
        [PermissionAuthorize("delete_employees")] // Requires delete permission
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            try
            {
                var result = await _attendanceService.DeleteAttendanceAsync(id, GetCurrentUserId());

                if (!result.IsSuccess)
                {
                    if (result.Message.Contains("not found"))
                        return NotFound(result);

                    return BadRequest(result);
                }

                _logger.LogWarning("Attendance record {Id} deleted by admin {AdminId}",
                    id, GetCurrentUserId());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attendance record {Id}", id);
                return StatusCode(500, ServiceResponse<bool>.FromException(
                    ex, "Failed to delete attendance record"));
            }
        }

        /// <summary>
        /// Bulk imports attendance records (Admin only).
        /// Used for migrating historical data or batch corrections.
        /// </summary>
        /// <param name="records">List of attendance records to import</param>
        /// <returns>Import result with count</returns>
        [HttpPost("admin/bulk-import")]
        [PermissionAuthorize("edit_attendance")]
        [ProducesResponseType(typeof(ServiceResponse<BulkImportResultDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BulkImportAttendance([FromBody] List<CreateAttendanceDTO> records)
        {
            try
            {
                if (records == null || records.Count == 0)
                {
                    return BadRequest(ServiceResponse<BulkImportResultDTO>.FailureResult(
                        "No records to import"));
                }

                if (records.Count > 1000)
                {
                    return BadRequest(ServiceResponse<BulkImportResultDTO>.FailureResult(
                        "Cannot import more than 1000 records at once"));
                }

                var result = await _attendanceService.BulkImportAttendanceAsync(records, GetCurrentUserId());

                _logger.LogInformation("Bulk import completed: {SuccessCount} successful, {FailCount} failed",
                    result.Data?.SuccessCount, result.Data?.FailCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                return StatusCode(500, ServiceResponse<BulkImportResultDTO>.FromException(
                    ex, "Failed to import attendance records"));
            }
        }

        #endregion

        #region Helper Methods

        private int GetEmployeeIdForCurrentUser()
        {
            // This would typically come from a service that maps user accounts to employees
            var employeeIdClaim = User.FindFirst("employee_id");
            if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out var employeeId))
                return employeeId;

            return 0;
        }

        private string GetDeviceInfo()
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var platform = Request.Headers["Sec-Ch-UA-Platform"].ToString();
            return $"{platform} - {userAgent}".Trim();
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        #endregion
    }
}
