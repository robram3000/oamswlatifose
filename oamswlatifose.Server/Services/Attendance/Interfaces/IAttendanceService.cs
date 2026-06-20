using oamswlatifose.Server.DTO.attendances;

namespace oamswlatifose.Server.Services.Attendance.Interfaces
{
    /// <summary>
    /// Service interface for attendance management operations providing comprehensive business logic
    /// for employee time tracking, attendance reporting, and administrative management of attendance records.
    /// This service orchestrates between repositories, applies business rules, and returns standardized responses.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Employee clock-in and clock-out operations with validation</para>
    /// <para>- Automatic calculation of hours worked and overtime</para>
    /// <para>- Attendance history retrieval with pagination and filtering</para>
    /// <para>- Daily, weekly, monthly attendance summaries</para>
    /// <para>- Late arrival detection and reporting</para>
    /// <para>- Absence tracking and reporting</para>
    /// <para>- Department-wide attendance analytics</para>
    /// <para>- Report generation with multiple formats (JSON, PDF, Excel)</para>
    /// <para>- Bulk import/export for administrative operations</para>
    /// <para>- Geolocation validation for clock-in/out</para>
    /// <para>- IP address tracking for security auditing</para>
    /// 
    /// <para>Business Rules:</para>
    /// <para>- Employees cannot clock in more than once per day</para>
    /// <para>- Clock-out time must be after clock-in time</para>
    /// <para>- Overtime calculated after 8 hours (configurable)</para>
    /// <para>- Late arrival threshold configurable per company policy</para>
    /// <para>- Weekend and holiday handling</para>
    /// <para>- Grace period for clock-in (configurable)</para>
    /// </summary>
    public interface IAttendanceService
    {
        #region Employee Self-Service Operations

        /// <summary>
        /// Records employee clock-in for the current day with geolocation validation.
        /// Automatically sets attendance date to today and records the time.
        /// Prevents duplicate clock-ins for the same day.
        /// </summary>
        /// <param name="clockInDto">Clock-in data including employee ID and optional geolocation</param>
        /// <param name="clientIp">Client IP address for security auditing</param>
        /// <returns>The created attendance record with calculated fields</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> ClockInAsync(ClockInDTO clockInDto, string clientIp);

        /// <summary>
        /// Records employee clock-out for the current day.
        /// Updates the existing attendance record with clock-out time and calculates hours worked.
        /// </summary>
        /// <param name="clockOutDto">Clock-out data including employee ID and optional geolocation</param>
        /// <param name="clientIp">Client IP address for security auditing</param>
        /// <returns>The updated attendance record with calculated hours and overtime</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> ClockOutAsync(ClockOutDTO clockOutDto, string clientIp);

        /// <summary>
        /// Gets the current day's attendance status for an employee.
        /// Shows whether clocked in, clocked out, or not started.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>Today's attendance record or null if not found</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> GetTodayAttendanceAsync(int employeeId);

        /// <summary>
        /// Gets paginated attendance history for a specific employee.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="pageNumber">Page number (1-indexed)</param>
        /// <param name="pageSize">Page size (max 100)</param>
        /// <returns>Paginated attendance history</returns>
        Task<ServiceResponse<PagedResult<AttendanceSummaryDTO>>> GetEmployeeAttendanceHistoryAsync(
            int employeeId,
            int pageNumber,
            int pageSize);

        /// <summary>
        /// Gets attendance summary for an employee within a date range.
        /// Includes total hours, overtime, days present, and attendance percentage.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Employee attendance summary statistics</returns>
        Task<ServiceResponse<EmployeeAttendanceSummaryDTO>> GetEmployeeAttendanceSummaryAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Gets attendance records for an employee within a specific date range.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>List of attendance records in the date range</returns>
        Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetEmployeeAttendanceByDateRangeAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        #endregion

        #region Manager/Admin Query Operations

        /// <summary>
        /// Gets paginated attendance records for all employees with filtering.
        /// Supports filtering by date range, department, status, and shift.
        /// </summary>
        /// <param name="filter">Attendance filter parameters</param>
        /// <returns>Paginated attendance records</returns>
        Task<ServiceResponse<PagedResult<AttendanceResponseDTO>>> GetAllAttendanceAsync(AttendanceFilterDTO filter);

        /// <summary>
        /// Gets a specific attendance record by ID.
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <returns>Attendance record</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> GetAttendanceByIdAsync(int id);

        /// <summary>
        /// Gets all attendance records for a specific date.
        /// </summary>
        /// <param name="date">Date to retrieve</param>
        /// <returns>List of attendance records for the date</returns>
        Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetAttendanceByDateAsync(DateTime date);

        /// <summary>
        /// Gets attendance records within a date range with optional department filtering.
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="department">Optional department filter</param>
        /// <returns>List of attendance records in the date range</returns>
        Task<ServiceResponse<IEnumerable<AttendanceResponseDTO>>> GetAttendanceByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            string department = null);

        /// <summary>
        /// Gets all employees who were absent on a specific date.
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>List of absent employees with details</returns>
        Task<ServiceResponse<IEnumerable<AbsentEmployeeDTO>>> GetAbsentEmployeesAsync(DateTime date);

        /// <summary>
        /// Gets employees who clocked in late on a specific date.
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <param name="lateThreshold">Time threshold considered late (default: 09:15)</param>
        /// <returns>List of late arrivals with details</returns>
        Task<ServiceResponse<IEnumerable<LateArrivalDTO>>> GetLateArrivalsAsync(
            DateTime date,
            TimeSpan lateThreshold);

        /// <summary>
        /// Gets attendance statistics for a specific department.
        /// </summary>
        /// <param name="department">Department name</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Department attendance statistics</returns>
        Task<ServiceResponse<DepartmentAttendanceStatsDTO>> GetDepartmentStatisticsAsync(
            string department,
            DateTime startDate,
            DateTime endDate);

        #endregion

        #region Report Generation

        /// <summary>
        /// Generates a comprehensive attendance report with summary and details.
        /// </summary>
        /// <param name="request">Report request parameters</param>
        /// <returns>Attendance report data</returns>
        Task<ServiceResponse<AttendanceReportDataDTO>> GenerateAttendanceReportAsync(AttendanceReportRequestDTO request);

        /// <summary>
        /// Exports attendance data to specified format (Excel, CSV, PDF).
        /// </summary>
        /// <param name="request">Export request parameters</param>
        /// <returns>File bytes with appropriate content type</returns>
        Task<ServiceResponse<byte[]>> ExportAttendanceAsync(AttendanceExportRequestDTO request);

        /// <summary>
        /// Gets attendance summary for dashboard display.
        /// Includes today's stats, trends, and alerts.
        /// </summary>
        /// <returns>Dashboard attendance summary</returns>
        Task<ServiceResponse<AttendanceDashboardDTO>> GetAttendanceDashboardAsync();

        #endregion

        #region Admin CRUD Operations

        /// <summary>
        /// Creates a new attendance record manually (admin only).
        /// Used for corrections or historical data entry.
        /// </summary>
        /// <param name="createDto">Attendance creation data</param>
        /// <param name="createdByUserId">ID of admin creating the record</param>
        /// <returns>Created attendance record</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> CreateAttendanceAsync(
            CreateAttendanceDTO createDto,
            int createdByUserId);

        /// <summary>
        /// Updates an existing attendance record (admin only).
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <param name="updateDto">Update data</param>
        /// <param name="updatedByUserId">ID of admin updating the record</param>
        /// <returns>Updated attendance record</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> UpdateAttendanceAsync(
            int id,
            UpdateAttendanceDTO updateDto,
            int updatedByUserId);

        /// <summary>
        /// Deletes an attendance record (admin only).
        /// This operation is irreversible.
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <param name="deletedByUserId">ID of admin deleting the record</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> DeleteAttendanceAsync(int id, int deletedByUserId);

        /// <summary>
        /// Bulk imports attendance records from external system or historical data.
        /// </summary>
        /// <param name="records">List of attendance records to import</param>
        /// <param name="importedByUserId">ID of admin performing the import</param>
        /// <returns>Import result with success/failure counts</returns>
        Task<ServiceResponse<BulkImportResultDTO>> BulkImportAttendanceAsync(
            List<CreateAttendanceDTO> records,
            int importedByUserId);

        /// <summary>
        /// Approves or rejects a pending attendance record (manager workflow).
        /// </summary>
        /// <param name="id">Attendance record ID</param>
        /// <param name="isApproved">Approval status</param>
        /// <param name="comments">Approval/rejection comments</param>
        /// <param name="approvedByUserId">ID of manager approving/rejecting</param>
        /// <returns>Updated attendance record</returns>
        Task<ServiceResponse<AttendanceResponseDTO>> ApproveAttendanceAsync(
            int id,
            bool isApproved,
            string comments,
            int approvedByUserId);

        #endregion

        #region Analytics and Calculations

        /// <summary>
        /// Calculates total hours worked by an employee in a date range.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Total hours worked</returns>
        Task<ServiceResponse<decimal>> CalculateTotalHoursWorkedAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Calculates total overtime hours by an employee in a date range.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Total overtime hours</returns>
        Task<ServiceResponse<decimal>> CalculateTotalOvertimeAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Calculates attendance percentage for an employee in a date range.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Attendance percentage (0-100)</returns>
        Task<ServiceResponse<double>> CalculateAttendancePercentageAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate);

        #endregion

        #region Validation and Utilities

        /// <summary>
        /// Checks if an employee is already clocked in for the day.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>True if already clocked in today</returns>
        Task<ServiceResponse<bool>> IsEmployeeClockedInTodayAsync(int employeeId);

        /// <summary>
        /// Gets the current attendance status of an employee.
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>Attendance status (ClockedIn, ClockedOut, NotStarted)</returns>
        Task<ServiceResponse<string>> GetEmployeeCurrentStatusAsync(int employeeId);

        /// <summary>
        /// Validates if a location is within allowed geofence for clock-in.
        /// </summary>
        /// <param name="latitude">Latitude</param>
        /// <param name="longitude">Longitude</param>
        /// <returns>True if location is valid</returns>
        Task<ServiceResponse<bool>> ValidateLocationAsync(double latitude, double longitude);

        #endregion
    }
}
