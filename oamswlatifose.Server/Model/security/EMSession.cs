using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.security
{
    [Table("EMSession")]
    public class EMSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionToken { get; set; }

        [Required]
        [MaxLength(45)]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [Required]
        public DateTime LoginTime { get; set; }

        public DateTime? LogoutTime { get; set; }

        public DateTime? LastActivity { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [MaxLength(50)]
        public string DeviceType { get; set; }

        [MaxLength(255)]
        public string Location { get; set; }

        public virtual EMAuthorizeruser User { get; set; }
    }
}