using oamswlatifose.Server.Validations.Attributes;
using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.attendances
{
    /// <summary>
    /// Detailed attendance response DTO with formatted time values and calculated fields.
    /// </summary>
    public class AttendanceResponseDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string DateFormatted { get; set; }
        public string DayOfWeek { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public string TimeInFormatted { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string TimeOutFormatted { get; set; }
        public string Status { get; set; }
        public string AttendanceStatus { get; set; }
        public string StatusColor { get; set; }
        public string Shift { get; set; }
        public decimal? HoursWorked { get; set; }
        public string HoursWorkedFormatted { get; set; }
        public decimal? OvertimeHours { get; set; }
        public string OvertimeFormatted { get; set; }
        public string Remarks { get; set; }
        public bool IsComplete { get; set; }
        public string CreatedAtFormatted { get; set; }

        // Location of the clock-in (geofence).
        public string WorkLocation { get; set; }   // Office / Outside / Unknown
        public string BranchName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// Summary DTO for attendance list views.
    /// </summary>
    public class AttendanceSummaryDTO
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string Date { get; set; }
        public string TimeIn { get; set; }
        public string TimeOut { get; set; }
        public string Status { get; set; }
        public string HoursWorked { get; set; }
        public string WorkLocation { get; set; }   // Office / Outside / Unknown (auto-mapped from entity)
    }

    /// <summary>
    /// DTO for creating new attendance records.
    /// </summary>
    public class CreateAttendanceDTO
    {
        [Required(ErrorMessage = "Employee ID is required")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Attendance date is required")]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? TimeIn { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? TimeOut { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; }

        [StringLength(10, ErrorMessage = "Shift cannot exceed 10 characters")]
        public string Shift { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string Remarks { get; set; }
    }

    /// <summary>
    /// DTO for updating existing attendance records.
    /// </summary>
    public class UpdateAttendanceDTO
    {
        [DataType(DataType.Time)]
        public TimeSpan? TimeIn { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? TimeOut { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; }

        [StringLength(10, ErrorMessage = "Shift cannot exceed 10 characters")]
        public string Shift { get; set; }

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters")]
        public string Remarks { get; set; }
    }

    /// <summary>
    /// DTO for employee clock-in operation.
    /// </summary>
    public class ClockInDTO
    {
        [Required(ErrorMessage = "Employee ID is required")]
        public int EmployeeId { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? TimeIn { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string DeviceInfo { get; set; }
    }

    /// <summary>
    /// DTO for employee clock-out operation.
    /// </summary>
    public class ClockOutDTO
    {
        [Required(ErrorMessage = "Employee ID is required")]
        public int EmployeeId { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? TimeOut { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    /// <summary>
    /// DTO for attendance report request parameters.
    /// </summary>
    public class AttendanceReportDTO
    {
        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date")]
        public DateTime EndDate { get; set; }

        public int? EmployeeId { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public string ReportFormat { get; set; } = "JSON";
    }
    /// <summary>
    /// Filter parameters for attendance queries
    /// </summary>
    public class AttendanceFilterDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? EmployeeId { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public string Shift { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "AttendanceDate";
        public string SortDirection { get; set; } = "DESC";
    }

    /// <summary>
    /// Attendance report request parameters
    /// </summary>
    public class AttendanceReportRequestDTO
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? EmployeeId { get; set; }
        public string Department { get; set; }
        public bool IncludeOvertime { get; set; } = true;
        public bool IncludeLateArrivals { get; set; } = true;
    }

    /// <summary>
    /// Attendance report data
    /// </summary>
    public class AttendanceReportDataDTO
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalDays { get; set; }
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLate { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public List<EmployeeAttendanceSummaryDTO> EmployeeSummaries { get; set; }
    }

    /// <summary>
    /// Employee attendance summary
    /// </summary>
    public class EmployeeAttendanceSummaryDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public int DaysLate { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public double AttendancePercentage { get; set; }
    }

    /// <summary>
    /// Late arrival information
    /// </summary>
    public class LateArrivalDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan TimeIn { get; set; }
        public TimeSpan ExpectedTime { get; set; }
        public int MinutesLate { get; set; }
    }

    /// <summary>
    /// Absent employee information
    /// </summary>
    public class AbsentEmployeeDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public bool HasApprovedLeave { get; set; }
    }

    /// <summary>
    /// Department attendance statistics
    /// </summary>
    public class DepartmentAttendanceStatsDTO
    {
        public string Department { get; set; }
        public int TotalEmployees { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalWorkingDays { get; set; }
        public decimal AttendanceRate { get; set; }
        public decimal AverageHoursPerDay { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; }
        public List<EmployeeAttendanceSummaryDTO> TopPerformers { get; set; }
    }

    /// <summary>
    /// Export request parameters
    /// </summary>
    public class AttendanceExportRequestDTO
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string Department { get; set; }
        public string Format { get; set; } = "excel";
        public bool IncludeSummary { get; set; } = true;
        public bool IncludeDetails { get; set; } = true;
    }

    /// <summary>
    /// Bulk import result
    /// </summary>
    public class BulkImportResultDTO
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Errors { get; set; }
        public List<int> CreatedIds { get; set; }
    }
    /// <summary>
    /// Dashboard attendance statistics for overview display
    /// </summary>
    public class AttendanceDashboardDTO
    {
        public DateTime Date { get; set; }
        public int TotalEmployees { get; set; }
        public DailyAttendanceStatsDTO TodayStats { get; set; }
        public PeriodAttendanceStatsDTO WeeklyStats { get; set; }
        public PeriodAttendanceStatsDTO MonthlyStats { get; set; }
        public List<AttendanceActivityDTO> RecentActivity { get; set; }
        public List<AttendanceAlertDTO> Alerts { get; set; }
    }

    /// <summary>
    /// Daily attendance statistics
    /// </summary>
    public class DailyAttendanceStatsDTO
    {
        public int Present { get; set; }
        public int Late { get; set; }
        public int Absent { get; set; }
        public int NotClockedIn { get; set; }
        public int ClockedIn { get; set; }
        public int Completed { get; set; }
        public double AttendanceRate => Present + Late > 0
            ? Math.Round((double)(Present + Late) / (Present + Late + Absent) * 100, 2)
            : 0;
    }

    /// <summary>
    /// Period attendance statistics (weekly/monthly)
    /// </summary>
    public class PeriodAttendanceStatsDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public double AverageDailyAttendance { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public decimal TotalOvertimeHours { get; set; }
    }

    /// <summary>
    /// Recent attendance activity
    /// </summary>
    public class AttendanceActivityDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Attendance alerts and notifications
    /// </summary>
    public class AttendanceAlertDTO
    {
        public string Type { get; set; } // Info, Warning, Critical
        public string Message { get; set; }
        public string Severity { get; set; } // Low, Medium, High
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
    }
}
