using oamswlatifose.Server.Model.occurance;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Repository.AttendanceManagement.Interfaces
{
    /// <summary>
    /// Interface for attendance data query operations providing comprehensive read-only access to employee attendance records.
    /// This repository interface defines contract methods for retrieving attendance information with advanced filtering
    /// capabilities essential for time tracking, payroll processing, and workforce management modules.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Daily, weekly, monthly, and custom date range attendance retrieval</para>
    /// <para>- Employee-specific attendance history with chronological ordering</para>
    /// <para>- Attendance status filtering (present, absent, late, half-day, etc.)</para>
    /// <para>- Overtime calculation and reporting queries</para>
    /// <para>- Shift-based attendance analysis and pattern recognition</para>
    /// <para>- Department-wide attendance summaries and statistical aggregations</para>
    /// 
    /// <para>All query methods are optimized for performance through appropriate indexing
    /// strategies and implement asynchronous patterns for non-blocking data access.</para>
    /// </summary>
    public interface IAttendanceTrackingQueryRepository
    {
        /// <summary>
        /// Retrieves all attendance records from the system without any date restrictions.
        /// Suitable for comprehensive reporting, data exports, and system-wide analytics.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of attendance records</returns>
        Task<IEnumerable<EMAttendance>> GetAllAttendanceRecordsAsync();

        /// <summary>
        /// Retrieves a specific attendance record using its unique system-generated identifier.
        /// Provides complete attendance details including timestamps, hours worked, and status information.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the attendance record</param>
        /// <returns>A task containing the attendance entity if found; otherwise, null reference</returns>
        Task<EMAttendance> GetAttendanceByIdAsync(int id);

        /// <summary>
        /// Retrieves complete attendance history for a specific employee with chronological ordering.
        /// Essential for employee self-service portals, manager reviews, and individual performance analysis.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee whose attendance records to retrieve</param>
        /// <returns>A task containing collection of attendance records for the specified employee, ordered by date descending</returns>
        Task<IEnumerable<EMAttendance>> GetAttendanceByEmployeeIdAsync(int employeeId);

        /// <summary>
        /// Retrieves paginated attendance records for a specific employee to efficiently display in UI grids.
        /// Implements server-side pagination to optimize performance when viewing large attendance histories.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee</param>
        /// <param name="pageNumber">The current page number (1-indexed) for pagination</param>
        /// <param name="pageSize">The number of attendance records to display per page</param>
        /// <returns>A task containing paginated collection of attendance records for the specified employee</returns>
        Task<IEnumerable<EMAttendance>> GetEmployeeAttendancePaginatedAsync(int employeeId, int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves all attendance records recorded on a specific calendar date across all employees.
        /// Critical for daily attendance reporting, absence tracking, and real-time workforce monitoring.
        /// </summary>
        /// <param name="date">The specific date for which to retrieve attendance records</param>
        /// <returns>A task containing collection of attendance records for the specified date</returns>
        Task<IEnumerable<EMAttendance>> GetAttendanceByDateAsync(DateTime date);

        /// <summary>
        /// Retrieves attendance records within a specified date range for comprehensive period analysis.
        /// Supports payroll cycles, monthly performance reviews, and attendance pattern analysis.
        /// </summary>
        /// <param name="startDate">The beginning date of the attendance period (inclusive)</param>
        /// <param name="endDate">The ending date of the attendance period (inclusive)</param>
        /// <returns>A task containing collection of attendance records falling within the specified date range</returns>
        Task<IEnumerable<EMAttendance>> GetAttendanceByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves attendance records for a specific employee within a defined date range.
        /// Used for individual attendance analysis, leave balance calculations, and timesheet verification.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee</param>
        /// <param name="startDate">The beginning date of the attendance period (inclusive)</param>
        /// <param name="endDate">The ending date of the attendance period (inclusive)</param>
        /// <returns>A task containing collection of employee's attendance records within the specified date range</returns>
        Task<IEnumerable<EMAttendance>> GetEmployeeAttendanceByDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves attendance records filtered by specific attendance status (Present, Absent, Late, Half-Day, etc.).
        /// Enables targeted analysis of attendance patterns and identification of recurring attendance issues.
        /// </summary>
        /// <param name="status">The attendance status value to filter records by</param>
        /// <returns>A task containing collection of attendance records with the specified status</returns>
        Task<IEnumerable<EMAttendance>> GetAttendanceByStatusAsync(string status);

        /// <summary>
        /// Retrieves attendance records filtered by work shift assignment.
        /// Supports shift-based workforce analysis and scheduling optimization.
        /// </summary>
        /// <param name="shift">The shift identifier (Morning, Evening, Night, etc.) to filter records by</param>
        /// <returns>A task containing collection of attendance records associated with the specified shift</returns>
        Task<IEnumerable<EMAttendance>> GetAttendanceByShiftAsync(string shift);

        /// <summary>
        /// Retrieves all employees who were absent on a specific date without valid attendance records.
        /// Critical for real-time absence tracking, follow-up actions, and attendance compliance monitoring.
        /// </summary>
        /// <param name="date">The date to check for employee absences</param>
        /// <returns>A task containing collection of employees who have no attendance record for the specified date</returns>
        Task<IEnumerable<EMEmployees>> GetAbsentEmployeesByDateAsync(DateTime date);

        /// <summary>
        /// Calculates the total hours worked by a specific employee within a date range.
        /// Essential for payroll calculations, productivity analysis, and labor cost allocation.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee</param>
        /// <param name="startDate">The beginning date of the calculation period (inclusive)</param>
        /// <param name="endDate">The ending date of the calculation period (inclusive)</param>
        /// <returns>A task containing the sum of hours worked by the employee in the specified period</returns>
        Task<decimal> GetTotalHoursWorkedByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Calculates the total overtime hours worked by a specific employee within a date range.
        /// Critical for overtime compensation, labor law compliance, and workforce cost management.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee</param>
        /// <param name="startDate">The beginning date of the calculation period (inclusive)</param>
        /// <param name="endDate">The ending date of the calculation period (inclusive)</param>
        /// <returns>A task containing the sum of overtime hours worked by the employee in the specified period</returns>
        Task<decimal> GetTotalOvertimeByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves employees with late check-in records on a specific date for disciplinary tracking.
        /// Enables identification of punctuality issues and supports attendance improvement initiatives.
        /// </summary>
        /// <param name="date">The date to check for late arrivals</param>
        /// <param name="lateThreshold">The time threshold considered as late arrival (e.g., 9:00 AM)</param>
        /// <returns>A task containing collection of attendance records where check-in time exceeded the specified threshold</returns>
        Task<IEnumerable<EMAttendance>> GetLateArrivalsByDateAsync(DateTime date, TimeSpan lateThreshold);

        /// <summary>
        /// Retrieves comprehensive attendance statistics for a department over a specified period.
        /// Provides aggregated metrics including average hours worked, total overtime, and attendance percentages.
        /// </summary>
        /// <param name="department">The department name for which to calculate attendance statistics</param>
        /// <param name="startDate">The beginning date of the analysis period (inclusive)</param>
        /// <param name="endDate">The ending date of the analysis period (inclusive)</param>
        /// <returns>A task containing departmental attendance statistics including averages and totals</returns>
        Task<object> GetDepartmentAttendanceStatisticsAsync(string department, DateTime startDate, DateTime endDate);
    }
}
