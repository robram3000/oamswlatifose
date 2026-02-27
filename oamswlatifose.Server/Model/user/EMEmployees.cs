using oamswlatifose.Server.Model.security;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.user
{
    [Table("EMEmployees")] 
    public class EMEmployees
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Column("EmployeeID")] 
        public int EmployeeID { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string Email { get; set; }

        [MaxLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public string Phone { get; set; }

        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Position { get; set; }
        [MaxLength(100)]

        [Column(TypeName = "nvarchar(100)")]
        public string Department { get; set; }


        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string City { get; set; }


        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; } 


        public virtual EMAuthorizeruser UserAccount { get; set; }
        public virtual ICollection<EMEmployees> Attendances { get; set; }

    }
}