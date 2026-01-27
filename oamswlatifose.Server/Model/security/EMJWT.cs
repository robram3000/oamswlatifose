using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.security
{
    [Table("EMJWT")]
    public class EMJWT
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Token { get; set; }

        [Required]
        [MaxLength(2000)]
        public string RefreshToken { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public DateTime RefreshTokenExpiresAt { get; set; }

        [Required]
        public bool IsRevoked { get; set; }

        [MaxLength(255)]
        public string RevokedReason { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(45)]
        public string IPAddress { get; set; }

        [MaxLength(500)]
        public string UserAgent { get; set; }

        public virtual EMAuthorizeruser User { get; set; }
    }
}