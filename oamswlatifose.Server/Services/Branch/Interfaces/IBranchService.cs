using oamswlatifose.Server.DTO.Branch;

namespace oamswlatifose.Server.Services.Branch.Interfaces
{
    /// <summary>
    /// Manages office/branch geofences and resolves a GPS point to "Office" (inside a branch
    /// radius) or "Outside" (off-site). This is the location side of attendance.
    /// </summary>
    public interface IBranchService
    {
        Task<ServiceResponse<List<BranchDTO>>> GetAllAsync(bool activeOnly = false);
        Task<ServiceResponse<BranchDTO>> SetAsync(SetBranchDTO dto);
        Task<ServiceResponse<bool>> DeleteAsync(int id);

        /// <summary>Classifies a coordinate against the active branches (Haversine distance).</summary>
        Task<LocationResolutionDTO> ResolveAsync(double? latitude, double? longitude);

        /// <summary>When true, clock-in is blocked unless the employee is inside a branch geofence.</summary>
        bool RequireOnSite { get; }
    }
}
