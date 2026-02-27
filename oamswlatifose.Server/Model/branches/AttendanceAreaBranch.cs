using oamswlatifose.Server.Model.user;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.branches
{
    [Table("EMAttendanceAreaBranch")]
    public class EMAttendanceAreaBranch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set;  }

        [Required]
        public int attendanceAreaId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? branchName { get; set; }

        [Required]
       
        public string? branchLocation { get; set; } 

        public string? branchCoordinates { get; set; }  

        public string branchType { get; set; }


        public virtual EMEmployees Employees { get; set; }
    }
}
