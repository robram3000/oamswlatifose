using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace oamswlatifose.Server.Model.branches
{
    /// <summary>
    /// An office/branch geofence: a centre point (<see cref="Latitude"/>/<see cref="Longitude"/>)
    /// and a <see cref="RadiusMeters"/>. A clock-in whose GPS falls inside the radius is recorded
    /// as "Office" for that branch; outside every active branch it is "Outside" (off-site).
    /// </summary>
    [Table("EMBranch")]
    public class EMBranch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        [Column(TypeName = "nvarchar(150)")]
        public string Name { get; set; }

        [MaxLength(250)]
        [Column(TypeName = "nvarchar(250)")]
        public string? Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        /// <summary>Geofence radius in metres.</summary>
        [Required]
        public int RadiusMeters { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
