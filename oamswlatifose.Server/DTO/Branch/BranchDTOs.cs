using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Branch
{
    /// <summary>A branch geofence as returned to clients.</summary>
    public class BranchDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusMeters { get; set; }
        public bool IsActive { get; set; }
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

        public bool IsActive { get; set; } = true;
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
