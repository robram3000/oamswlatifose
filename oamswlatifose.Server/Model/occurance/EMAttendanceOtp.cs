using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.occurance
{
    /// <summary>
    /// A one-time password issued to confirm an attendance action (clock-in / clock-out).
    /// When the employee taps "Time In" we capture the tap time in <see cref="RequestedTime"/>,
    /// email a code, and only write the actual <c>EMAttendance</c> row once that code is
    /// verified — so the recorded time-in is the moment they tapped, not the moment they typed
    /// the code.
    /// </summary>
    [Table("EMAttendanceOtp")]
    public class EMAttendanceOtp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [MaxLength(255)]

        public string Email { get; set; }

        [Required]
        [MaxLength(10)]

        public string Code { get; set; }

        /// <summary>"ClockIn" or "ClockOut".</summary>
        [Required]
        [MaxLength(20)]

        public string Purpose { get; set; }

        /// <summary>The time-of-day the action was initiated (the value recorded on success).</summary>
        [Column(TypeName = "time")]
        public TimeSpan RequestedTime { get; set; }

        // Location captured at request time, persisted onto the attendance row on verify.
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [MaxLength(20)]

        public string? WorkLocation { get; set; }

        public int? BranchId { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public int Attempts { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
