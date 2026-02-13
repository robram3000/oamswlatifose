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
}
