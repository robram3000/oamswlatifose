using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Branch
{
    /// <summary>A single geofence vertex / coordinate.</summary>
    public class GeoPointDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    /// <summary>A branch geofence as returned to clients.</summary>
    public class BranchDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; }
        /// <summary>"circle" or "polygon" — the geofence shape this branch validates against.</summary>
        public string GeofenceType { get; set; } = "circle";
        /// <summary>Polygon vertices (empty for circular branches).</summary>
        public List<GeoPointDTO> Polygon { get; set; } = new();
        public bool IsActive { get; set; }
        public List<EmployeeRefDTO> AssignedEmployees { get; set; } = new();
    }

    /// <summary>Minimal employee reference used inside BranchDTO.</summary>
    public class EmployeeRefDTO
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
    }

    /// <summary>Create-or-update payload for a branch geofence (admin).</summary>
    public class SetBranchDTO
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Branch name is required")]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Address { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double Longitude { get; set; }

        [Range(10, 100000, ErrorMessage = "Radius must be between 10 and 100000 metres")]
        public int RadiusMeters { get; set; } = 100;

        /// <summary>
        /// Optional polygon geofence. When 3 or more points are supplied the branch validates by
        /// point-in-polygon instead of radius; <see cref="Latitude"/>/<see cref="Longitude"/> are
        /// then treated as the map centre (the service recomputes them to the polygon centroid).
        /// </summary>
        public List<GeoPointDTO> Polygon { get; set; } = new();

        public bool IsActive { get; set; } = true;

        /// <summary>EMEmployees.Id values to assign to this branch. Replaces the full set.</summary>
        public List<int> EmployeeIds { get; set; } = new();
    }

    /// <summary>
    /// The outcome of checking a GPS point against the active branches: whether it is on-site,
    /// which branch, and how far. Shared by the clock-in flow.
    /// </summary>
    public class LocationResolutionDTO
    {
        public bool OnSite { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        /// <summary>"Office", "Outside", or "Unknown" (no coordinates supplied).</summary>
        public string WorkLocation { get; set; } = "Unknown";
        public int? DistanceMeters { get; set; }
    }
}
