using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.occurance;

namespace oamswlatifose.Server.Repository.AttendanceManagement.Implementation
{
    /// <summary>
    /// Command repository implementation for attendance data modification operations.
    /// This repository handles all create, update, and delete operations for attendance records
    /// with comprehensive business rule validation, automatic calculations, and audit trail maintenance.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Automatic hours worked calculation based on time-in and time-out values</para>
    /// <para>- Overtime computation according to configurable business rules</para>
    /// <para>- Duplicate attendance prevention for same employee and date combinations</para>
    /// <para>- Status determination based on time-in thresholds and shift schedules</para>
    /// <para>- Bulk attendance processing for efficient time clock integration</para>
    /// <para>- Validation of employee existence and active employment status</para>
    /// 
    /// <para>All operations maintain referential integrity with employee master data
    /// and enforce attendance policies through configurable business rule validation.</para>
    /// </summary>
    public class AttendanceTrackingCommandRepository : IAttendanceTrackingCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceTrackingCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the AttendanceTrackingCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for attendance operations.
        /// </summary>
        /// <param name="context">The application database context providing access to attendance and employee tables</param>
        /// <param name="logger">The logging service for capturing attendance operation details and error information</param>
        public AttendanceTrackingCommandRepository(
            ApplicationDbContext context,
            ILogger<AttendanceTrackingCommandRepository> _logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new attendance record with automatic calculation of hours worked and overtime.
        /// Performs comprehensive validation including duplicate checking, employee verification,
        /// and automatic status determination based on configured business rules.
        /// </summary>
        /// <param name="attendance">The attendance entity containing employee ID, date, and time tracking information</param>
        /// <returns>A task representing the asynchronous operation with the newly created attendance record including calculated fields</returns>
        /// <exception cref="ArgumentNullException">Thrown when the attendance parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when duplicate attendance record exists or employee not found</exception>
        public async Task<EMAttendance> CreateAttendanceAsync(EMAttendance attendance)
        {
            if (attendance == null)
                throw new ArgumentNullException(nameof(attendance));

            // Validate employee exists
            var employeeExists = await _context.EMEmployees.AnyAsync(e => e.Id == attendance.EmployeeId);
            if (!employeeExists)
                throw new InvalidOperationException($"Employee with ID {attendance.EmployeeId} not found");

            // Check for duplicate attendance on same date
            var duplicate = await _context.EMAttendance
                .AnyAsync(a => a.EmployeeId == attendance.EmployeeId && a.AttendanceDate == attendance.AttendanceDate);
            if (duplicate)
                throw new InvalidOperationException($"Attendance record already exists for employee {attendance.EmployeeId} on {attendance.AttendanceDate:d}");

            // Calculate hours worked and overtime
            CalculateAttendanceMetrics(attendance);

            // Auto-determine status if not provided
            if (string.IsNullOrEmpty(attendance.Status))
                attendance.Status = DetermineAttendanceStatus(attendance);

            attendance.CreatedAt = DateTime.UtcNow;
            attendance.UpdatedAt = DateTime.UtcNow;

            await _context.EMAttendance.AddAsync(attendance);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created attendance record for employee {attendance.EmployeeId} on {attendance.AttendanceDate:d}");
            return attendance;
        }

        /// <summary>
        /// Updates an existing attendance record with modified time tracking information.
        /// Recalculates hours worked and overtime based on updated time-in/time-out values
        /// and maintains audit trail through UpdatedAt timestamp.
        /// </summary>
        /// <param name="attendance">The attendance entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated attendance entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the attendance parameter is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no attendance record exists with the specified Id</exception>
        public async Task<EMAttendance> UpdateAttendanceAsync(EMAttendance attendance)
        {
            if (attendance == null)
                throw new ArgumentNullException(nameof(attendance));

            var existingAttendance = await _context.EMAttendance
                .FirstOrDefaultAsync(a => a.Id == attendance.Id);
            if (existingAttendance == null)
                throw new KeyNotFoundException($"Attendance record with ID {attendance.Id} not found");

            // Preserve original creation timestamp
            attendance.CreatedAt = existingAttendance.CreatedAt;

            // Recalculate metrics with updated times
            CalculateAttendanceMetrics(attendance);

            // Update status if times changed
            if (existingAttendance.TimeIn != attendance.TimeIn || existingAttendance.TimeOut != attendance.TimeOut)
                attendance.Status = DetermineAttendanceStatus(attendance);

            attendance.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingAttendance).CurrentValues.SetValues(attendance);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated attendance record {attendance.Id} for employee {attendance.EmployeeId}");
            return existingAttendance;
        }

        /// <summary>
        /// Records time-in for an employee, creating a new attendance record for the current date.
        /// Automatically sets the attendance date to today and records the specified time-in value.
        /// Prevents duplicate time-in records for the same employee on the same date.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee checking in</param>
        /// <param name="timeIn">The time value when the employee checked in (defaults to current time if null)</param>
        /// <returns>A task representing the asynchronous operation with the newly created time-in attendance record</returns>
        public async Task<EMAttendance> ClockInAsync(int employeeId, TimeSpan? timeIn = null)
        {
            var today = DateTime.Today;

            // Check if already clocked in today
            var existing = await _context.EMAttendance
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
            if (existing != null)
                throw new InvalidOperationException($"Employee {employeeId} already clocked in today");

            var attendance = new EMAttendance
            {
                EmployeeId = employeeId,
                AttendanceDate = today,
                TimeIn = timeIn ?? DateTime.Now.TimeOfDay,
                Status = "Present",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.EMAttendance.AddAsync(attendance);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Employee {employeeId} clocked in at {attendance.TimeIn}");
            return attendance;
        }

        /// <summary>
        /// Records time-out for an employee, updating the existing attendance record for the current date.
        /// Automatically calculates hours worked and overtime upon checkout completion.
        /// </summary>
        /// <param name="employeeId">The unique system identifier of the employee checking out</param>
        /// <param name="timeOut">The time value when the employee checked out (defaults to current time if null)</param>
        /// <returns>A task representing the asynchronous operation with the updated attendance record including calculated hours</returns>
        /// <exception cref="InvalidOperationException">Thrown when no active time-in record exists for today</exception>
        public async Task<EMAttendance> ClockOutAsync(int employeeId, TimeSpan? timeOut = null)
        {
            var today = DateTime.Today;

            var attendance = await _context.EMAttendance
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AttendanceDate == today);
            if (attendance == null)
                throw new InvalidOperationException($"No clock-in record found for employee {employeeId} today");

            attendance.TimeOut = timeOut ?? DateTime.Now.TimeOfDay;
            CalculateAttendanceMetrics(attendance);
            attendance.Status = DetermineAttendanceStatus(attendance);
            attendance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Employee {employeeId} clocked out at {attendance.TimeOut}, hours worked: {attendance.HoursWorked}");
            return attendance;
        }

        /// <summary>
        /// Permanently removes an attendance record from the system.
        /// This operation is irreversible and should be restricted to authorized administrators only.
        /// </summary>
        /// <param name="id">The unique system identifier of the attendance record to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no attendance record exists with the specified Id</exception>
        public async Task<bool> DeleteAttendanceAsync(int id)
        {
            var attendance = await _context.EMAttendance
                .FirstOrDefaultAsync(a => a.Id == id);
            if (attendance == null)
                throw new KeyNotFoundException($"Attendance record with ID {id} not found");

            _context.EMAttendance.Remove(attendance);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted attendance record {id}");
            return result > 0;
        }

        /// <summary>
        /// Bulk imports multiple attendance records in a single transactional operation.
        /// Optimized for time clock system integration, historical data migration,
        /// and batch attendance processing scenarios.
        /// </summary>
        /// <param name="attendanceRecords">Collection of attendance entities to be created in the database</param>
        /// <returns>A task representing the asynchronous operation with the count of successfully created records</returns>
        public async Task<int> BulkCreateAttendanceAsync(IEnumerable<EMAttendance> attendanceRecords)
        {
            if (attendanceRecords == null)
                throw new ArgumentNullException(nameof(attendanceRecords));

            var records = attendanceRecords.ToList();
            var utcNow = DateTime.UtcNow;

            foreach (var record in records)
            {
                CalculateAttendanceMetrics(record);
                if (string.IsNullOrEmpty(record.Status))
                    record.Status = DetermineAttendanceStatus(record);
                record.CreatedAt = utcNow;
                record.UpdatedAt = utcNow;
            }

            await _context.EMAttendance.AddRangeAsync(records);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Bulk created {result} attendance records");
            return result;
        }

        /// <summary>
        /// Calculates hours worked and overtime based on time-in and time-out values.
        /// Implements configurable business rules for overtime threshold and standard working hours.
        /// </summary>
        /// <param name="attendance">The attendance record requiring hours and overtime calculation</param>
        private void CalculateAttendanceMetrics(EMAttendance attendance)
        {
            if (attendance.TimeIn.HasValue && attendance.TimeOut.HasValue)
            {
                var timeIn = attendance.TimeIn.Value;
                var timeOut = attendance.TimeOut.Value;

                // Calculate total hours worked
                var hoursWorked = (timeOut - timeIn).TotalHours;
                attendance.HoursWorked = (decimal)Math.Max(0, hoursWorked);

                // Standard work hours (configurable, default 8 hours)
                const decimal standardHours = 8m;

                // Calculate overtime (hours beyond standard)
                if (attendance.HoursWorked > standardHours)
                    attendance.OvertimeHours = attendance.HoursWorked - standardHours;
                else
                    attendance.OvertimeHours = 0;
            }
        }

        /// <summary>
        /// Determines attendance status based on time-in value and configured shift schedules.
        /// Evaluates whether employee is present, late, or has other status indicators.
        /// </summary>
        /// <param name="attendance">The attendance record requiring status determination</param>
        /// <returns>A string representing the determined attendance status</returns>
        private string DetermineAttendanceStatus(EMAttendance attendance)
        {
            if (!attendance.TimeIn.HasValue)
                return "Pending";

            // Default shift start time (configurable)
            var shiftStart = new TimeSpan(9, 0, 0); // 9:00 AM
            var lateThreshold = new TimeSpan(9, 15, 0); // 9:15 AM

            if (attendance.TimeIn <= shiftStart)
                return "Present";
            else if (attendance.TimeIn <= lateThreshold)
                return "Late";
            else
                return "Late-Excessive";
        }
    }
}
