using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.security
{
    [Table("EMRoleBasedAccessControl")]
    public class EMRoleBasedAccessControl
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RoleName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        public bool CanViewEmployees { get; set; }

        [Required]
        public bool CanEditEmployees { get; set; }

        [Required]
        public bool CanDeleteEmployees { get; set; }

        [Required]
        public bool CanViewAttendance { get; set; }

        [Required]
        public bool CanEditAttendance { get; set; }

        [Required]
        public bool CanGenerateReports { get; set; }

        [Required]
        public bool CanManageUsers { get; set; }

        [Required]
        public bool CanManageRoles { get; set; }

        [Required]
        public bool CanAccessAdminPanel { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<EMAuthorizeruser> Users { get; set; }
    }
}