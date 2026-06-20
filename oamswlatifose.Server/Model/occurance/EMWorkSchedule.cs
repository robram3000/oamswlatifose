using oamswlatifose.Server.Model.user;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.occurance
{
    /// <summary>
    /// A per-employee work schedule. The scheduled <see cref="StartTime"/> (plus the
    /// <see cref="GraceMinutes"/> grace window) is what attendance is measured against:
    /// a clock-in at or before StartTime + grace is "Present", otherwise "Late".
    /// One active row per employee (enforced by a unique index on EmployeeId).
    /// </summary>
    [Table("EMWorkSchedule")]
    public class EMWorkSchedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }

        /// <summary>Scheduled time-in (the time the employee is expected to clock in).</summary>
        [Required]
        [Column(TypeName = "time")]
        public TimeSpan StartTime { get; set; }

        /// <summary>Scheduled time-out.</summary>
        [Required]
        [Column(TypeName = "time")]
        public TimeSpan EndTime { get; set; }

        /// <summary>Minutes after <see cref="StartTime"/> still counted as on-time.</summary>
        public int GraceMinutes { get; set; } = 5;

        /// <summary>Comma-separated working days, e.g. "Mon,Tue,Wed,Thu,Fri".</summary>
        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string WorkDays { get; set; } = "Mon,Tue,Wed,Thu,Fri";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual EMEmployees Employee { get; set; }
    }
}
