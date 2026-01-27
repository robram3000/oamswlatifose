using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.security
{
    [Table("EMAuthLog")]
    public class EMAuthLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int? UserId { get; set; }

        [MaxLength(100)]
        public string UsernameAttempted { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [MaxLength(45)]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        [MaxLength(1000)]
        public string Details { get; set; }

        [Required]
        public bool WasSuccessful { get; set; }

        [MaxLength(255)]
        public string FailureReason { get; set; }

        [MaxLength(50)]
        public string DeviceType { get; set; }

        [MaxLength(255)]
        public string Location { get; set; }

        public virtual EMAuthorizeruser User { get; set; }
    }
}