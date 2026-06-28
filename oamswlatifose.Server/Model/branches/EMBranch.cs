using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using oamswlatifose.Server.Model.user;

namespace oamswlatifose.Server.Model.branches
{
    /// <summary>
    /// An office/branch geofence. Two shapes are supported:
    /// <list type="bullet">
    /// <item>Circular — a centre point (<see cref="Latitude"/>/<see cref="Longitude"/>) and a
    /// <see cref="RadiusMeters"/>; membership is a Haversine distance ≤ radius test.</item>
    /// <item>Polygon — an ordered ring of vertices in <see cref="PolygonJson"/> (≥3 points);
    /// membership is a point-in-polygon test. The centre is kept as the polygon centroid for map
    /// framing.</item>
    /// </list>
    /// A clock-in whose GPS falls inside the geofence is recorded as "Office" for that branch;
    /// outside every active branch it is "Outside" (off-site).
    /// </summary>
    [Table("EMBranch")]
    public class EMBranch
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]

        public string Name { get; set; }

        [MaxLength(250)]

        public string? Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        /// <summary>Geofence radius in metres. Used only for circular branches (ignored when a polygon is set).</summary>
        [Required]
        public int RadiusMeters { get; set; } = 100;

        /// <summary>
        /// Optional polygon geofence, stored as a JSON array of <c>[latitude, longitude]</c> pairs,
        /// e.g. <c>[[14.60,120.98],[14.61,120.98],[14.61,120.99]]</c>. When present with ≥3 points,
        /// validation uses a point-in-polygon test instead of the radius. Null/empty ⇒ circular branch.
        /// </summary>
        public string? PolygonJson { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<EMEmployees> Employees { get; set; } = new List<EMEmployees>();
    }
}
