using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Model.security
{
    [Table("EMAuthorizeruser")]
    public class EMAuthorizeruser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordSalt { get; set; }

        [Required]
        [ForeignKey("Role")]
        public int RoleId { get; set; }

        [ForeignKey("Employee")]
        public int? EmployeeId { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime? EmailVerifiedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public int FailedLoginAttempts { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [MaxLength(500)]
        public string PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpires { get; set; }


        // Foreign key realtion ship
        public virtual EMRoleBasedAccessControl Role { get; set; }
        public virtual EMEmployees Employee { get; set; }
        public virtual ICollection<EMSession> Sessions { get; set; }
        public virtual ICollection<EMAuthLog> AuthLogs { get; set; }
    }
}