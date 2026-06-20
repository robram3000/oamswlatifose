using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Schedule
{
    /// <summary>
    /// A work schedule as returned to clients. Times are formatted "HH:mm" (24h)
    /// so the frontend can render and round-trip them without TimeSpan parsing.
    /// </summary>
    public class WorkScheduleDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string StartTime { get; set; }   // "HH:mm"
        public string EndTime { get; set; }      // "HH:mm"
        public int GraceMinutes { get; set; }
        public string WorkDays { get; set; }     // "Mon,Tue,Wed,Thu,Fri"
        public bool IsActive { get; set; }

        /// <summary>StartTime + grace, "HH:mm" — the cutoff after which a clock-in is "Late".</summary>
        public string LateAfter { get; set; }
    }

    /// <summary>
    /// Create-or-update payload for an employee's schedule (admin). EmployeeId is
    /// optional for the "set my own" convenience endpoint, where it is taken from the token.
    /// </summary>
    public class SetWorkScheduleDTO
    {
        public int? EmployeeId { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Start time must be HH:mm (24h)")]
        public string StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "End time must be HH:mm (24h)")]
        public string EndTime { get; set; }

        [Range(0, 120, ErrorMessage = "Grace minutes must be between 0 and 120")]
        public int GraceMinutes { get; set; } = 5;

        [MaxLength(50)]
        public string WorkDays { get; set; } = "Mon,Tue,Wed,Thu,Fri";
    }
}
