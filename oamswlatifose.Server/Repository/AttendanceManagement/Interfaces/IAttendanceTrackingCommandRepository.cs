using oamswlatifose.Server.Model.occurance;

namespace oamswlatifose.Server.Repository.AttendanceManagement.Interfaces
{
    /// <summary>
    /// Interface for attendance data modification operations defining contracts for all create,
    /// update, and delete operations on attendance entities. This repository interface establishes
    /// the pattern for attendance data persistence with comprehensive business rule enforcement.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Single attendance record creation with automatic hours calculation</para>
    /// <para>- Time-in and time-out operations for real-time attendance tracking</para>
    /// <para>- Attendance record updates with recalculated metrics</para>
    /// <para>- Permanent removal of attendance records</para>
    /// <para>- Bulk operations for efficient time clock system integration</para>
    /// 
    /// <para>All methods enforce attendance policies, prevent duplicate entries,
    /// and maintain accurate time tracking calculations. Implementations must handle
    /// concurrent clock-in/out operations and provide appropriate error feedback.</para>
    /// </summary>
    public interface IAttendanceTrackingCommandRepository
    {
        /// <summary>
        /// Creates a new attendance record with comprehensive validation and automatic calculations.
        /// Performs hours worked and overtime computation based on time-in and time-out values.
        /// </summary>
        /// <param name="attendance">The attendance entity containing employee identification and time tracking information</param>
        /// <returns>A task representing the asynchronous operation with the newly created attendance record including calculated fields</returns>
        Task<EMAttendance> CreateAttendanceAsync(EMAttendance attendance);

        /// <summary>
        /// Updates an existing attendance record with modified time tracking data.
        /// Automatically recalculates hours worked and overtime based on updated time values.
        /// </summary>
        /// <param name="attendance">The attendance entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated attendance entity</returns>
        Task<EMAttendance> UpdateAttendanceAsync(EMAttendance attendance);

        /// <summary>
        /// Records employee check-in time for the current workday, creating a new attendance record.
        /// Implements business rules to prevent duplicate check-ins and validate employee status.
        /// </summary>
        /// <param name="employeeId">The unique identifier of the employee checking in</param>
        /// <param name="timeIn">The time of check-in; defaults to current system time if not specified</param>
        /// <returns>A task representing the asynchronous operation with the newly created attendance record</returns>
        Task<EMAttendance> ClockInAsync(int employeeId, TimeSpan? timeIn = null);

        /// <summary>
        /// Records employee check-out time, completing the attendance record for the current workday.
        /// Automatically calculates total hours worked and overtime upon checkout completion.
        /// </summary>
        /// <param name="employeeId">The unique identifier of the employee checking out</param>
        /// <param name="timeOut">The time of check-out; defaults to current system time if not specified</param>
        /// <returns>A task representing the asynchronous operation with the updated attendance record</returns>
        Task<EMAttendance> ClockOutAsync(int employeeId, TimeSpan? timeOut = null);

        /// <summary>
        /// Permanently removes an attendance record from the system.
        /// This operation is irreversible and should be protected by appropriate authorization controls.
        /// </summary>
        /// <param name="id">The unique system identifier of the attendance record to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> DeleteAttendanceAsync(int id);

        /// <summary>
        /// Efficiently creates multiple attendance records in a single transactional operation.
        /// Optimized for bulk import scenarios, time clock system synchronization, and data migration.
        /// </summary>
        /// <param name="attendanceRecords">Collection of attendance entities to be created in the database</param>
        /// <returns>A task representing the asynchronous operation with the count of successfully created records</returns>
        Task<int> BulkCreateAttendanceAsync(IEnumerable<EMAttendance> attendanceRecords);
    }
}
