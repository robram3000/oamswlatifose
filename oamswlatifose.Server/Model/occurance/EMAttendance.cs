using oamswlatifose.Server.Model.user;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.occurance
{
    [Table("EMAttendance")]
    public class EMAttendance
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime AttendanceDate { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? TimeIn { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? TimeOut { get; set; }

        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string Status { get; set; } 

        [MaxLength(10)]
        [Column(TypeName = "nvarchar(10)")]
        public string Shift { get; set; }

        public decimal? HoursWorked { get; set; }

        public decimal? OvertimeHours { get; set; }

        [MaxLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string Remarks { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual EMEmployees Employee { get; set; }
    }
}