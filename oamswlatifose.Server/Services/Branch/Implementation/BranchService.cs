using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.DTO.Branch;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.branches;
using oamswlatifose.Server.Services.Branch.Interfaces;

namespace oamswlatifose.Server.Services.Branch.Implementation
{
    /// <summary>EF-backed branch store + geofence resolver.</summary>
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BranchService> _logger;

        public bool RequireOnSite { get; }

        public BranchService(ApplicationDbContext db, IConfiguration configuration, ILogger<BranchService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RequireOnSite = configuration.GetValue<bool>("AttendanceGeofence:RequireOnSite");
        }

        public async Task<ServiceResponse<List<BranchDTO>>> GetAllAsync(bool activeOnly = false)
        {
            try
            {
                var query = _db.EMBranches.AsQueryable();
                if (activeOnly) query = query.Where(b => b.IsActive);

                var list = await query.OrderBy(b => b.Name).ToListAsync();
                return ServiceResponse<List<BranchDTO>>.SuccessResult(list.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing branches");
                return ServiceResponse<List<BranchDTO>>.FromException(ex, "Failed to list branches");
            }
        }

        public async Task<ServiceResponse<BranchDTO>> SetAsync(SetBranchDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return ServiceResponse<BranchDTO>.FailureResult("Branch name is required");

                EMBranch branch;
                if (dto.Id is > 0)
                {
                    branch = await _db.EMBranches.FirstOrDefaultAsync(b => b.Id == dto.Id);
                    if (branch == null)
                        return ServiceResponse<BranchDTO>.FailureResult($"Branch {dto.Id} not found");
                    branch.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    branch = new EMBranch { CreatedAt = DateTime.UtcNow };
                    _db.EMBranches.Add(branch);
                }

                branch.Name = dto.Name.Trim();
                branch.Address = dto.Address?.Trim();
                branch.Latitude = dto.Latitude;
                branch.Longitude = dto.Longitude;
                branch.RadiusMeters = dto.RadiusMeters;
                branch.IsActive = dto.IsActive;

                await _db.SaveChangesAsync();
                return ServiceResponse<BranchDTO>.SuccessResult(ToDto(branch), "Branch saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving branch");
                return ServiceResponse<BranchDTO>.FromException(ex, "Failed to save branch");
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var branch = await _db.EMBranches.FirstOrDefaultAsync(b => b.Id == id);
                if (branch == null)
                    return ServiceResponse<bool>.FailureResult("Branch not found");

                _db.EMBranches.Remove(branch);
                await _db.SaveChangesAsync();
                return ServiceResponse<bool>.SuccessResult(true, "Branch deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch {Id}", id);
                return ServiceResponse<bool>.FromException(ex, "Failed to delete branch");
            }
        }

        public async Task<LocationResolutionDTO> ResolveAsync(double? latitude, double? longitude)
        {
            // No coordinates → we can't place the employee.
            if (latitude is null || longitude is null)
                return new LocationResolutionDTO { OnSite = false, WorkLocation = "Unknown" };

            var branches = await _db.EMBranches.Where(b => b.IsActive).ToListAsync();
            if (branches.Count == 0)
                return new LocationResolutionDTO { OnSite = false, WorkLocation = "Outside" };

            EMBranch nearest = null;
            double nearestDist = double.MaxValue;
            foreach (var b in branches)
            {
                var d = Haversine(latitude.Value, longitude.Value, b.Latitude, b.Longitude);
                if (d < nearestDist) { nearestDist = d; nearest = b; }
            }

            var onSite = nearest != null && nearestDist <= nearest.RadiusMeters;
            return new LocationResolutionDTO
            {
                OnSite = onSite,
                BranchId = onSite ? nearest.Id : (int?)null,
                BranchName = onSite ? nearest.Name : null,
                WorkLocation = onSite ? "Office" : "Outside",
                DistanceMeters = (int)Math.Round(nearestDist),
            };
        }

        /// <summary>Great-circle distance between two coordinates, in metres.</summary>
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0; // Earth radius (m)
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        private static BranchDTO ToDto(EMBranch b) => new()
        {
            Id = b.Id,
            Name = b.Name,
            Address = b.Address,
            Latitude = b.Latitude,
            Longitude = b.Longitude,
            RadiusMeters = b.RadiusMeters,
            IsActive = b.IsActive,
        };
    }
}
