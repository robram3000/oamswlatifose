using System.Text.Json;
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
                var query = _db.EMBranches.Include(b => b.Employees).AsQueryable();
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
                    branch = await _db.EMBranches
                        .Include(b => b.Employees)
                        .FirstOrDefaultAsync(b => b.Id == dto.Id);
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
                branch.RadiusMeters = dto.RadiusMeters;
                branch.IsActive = dto.IsActive;

                // Polygon branch when ≥3 vertices are supplied; otherwise a circular branch.
                var polygon = dto.Polygon ?? new List<GeoPointDTO>();
                if (polygon.Count >= 3)
                {
                    branch.PolygonJson = SerializePolygon(polygon);
                    // Keep the stored centre as the polygon centroid so maps frame it correctly.
                    var (cLat, cLng) = Centroid(polygon);
                    branch.Latitude = cLat;
                    branch.Longitude = cLng;
                }
                else
                {
                    branch.PolygonJson = null;
                    branch.Latitude = dto.Latitude;
                    branch.Longitude = dto.Longitude;
                }

                await _db.SaveChangesAsync();

                // Assign employees: unassign previous members not in the new list, assign new ones.
                var newIds = dto.EmployeeIds ?? new List<int>();

                // Employees currently assigned to this branch that are no longer in the list
                var toUnassign = await _db.EMEmployees
                    .Where(e => e.BranchId == branch.Id && !newIds.Contains(e.Id))
                    .ToListAsync();
                foreach (var e in toUnassign) e.BranchId = null;

                // Employees in the new list — assign them (covers both new and already-assigned)
                if (newIds.Count > 0)
                {
                    var toAssign = await _db.EMEmployees
                        .Where(e => newIds.Contains(e.Id))
                        .ToListAsync();
                    foreach (var e in toAssign) e.BranchId = branch.Id;
                }

                await _db.SaveChangesAsync();

                // Reload with employees for the response DTO
                await _db.Entry(branch).Collection(b => b.Employees).LoadAsync();

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

                // Unassign employees before deleting
                var employees = await _db.EMEmployees.Where(e => e.BranchId == id).ToListAsync();
                foreach (var e in employees) e.BranchId = null;
                await _db.SaveChangesAsync();

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

            double lat = latitude.Value, lng = longitude.Value;

            EMBranch nearest = null;        // closest centre, for the "Outside" distance readout
            double nearestDist = double.MaxValue;
            EMBranch containing = null;     // first branch whose geofence actually contains the point
            double containingDist = 0;

            foreach (var b in branches)
            {
                var d = Haversine(lat, lng, b.Latitude, b.Longitude);
                if (d < nearestDist) { nearestDist = d; nearest = b; }

                var polygon = DeserializePolygon(b.PolygonJson);
                bool inside = polygon.Count >= 3
                    ? PointInPolygon(lat, lng, polygon)   // polygon branch
                    : d <= b.RadiusMeters;                // circular branch

                if (inside && containing == null) { containing = b; containingDist = d; }
            }

            if (containing != null)
            {
                return new LocationResolutionDTO
                {
                    OnSite = true,
                    BranchId = containing.Id,
                    BranchName = containing.Name,
                    WorkLocation = "Office",
                    DistanceMeters = (int)Math.Round(containingDist),
                };
            }

            return new LocationResolutionDTO
            {
                OnSite = false,
                WorkLocation = "Outside",
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

        /// <summary>
        /// Ray-casting point-in-polygon test. Treats longitude as x and latitude as y; accurate for
        /// the small areas a work-zone polygon covers (no spherical correction needed at that scale).
        /// </summary>
        private static bool PointInPolygon(double lat, double lng, List<GeoPointDTO> poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                double yi = poly[i].Latitude, xi = poly[i].Longitude;
                double yj = poly[j].Latitude, xj = poly[j].Longitude;
                bool intersects = ((yi > lat) != (yj > lat))
                    && (lng < (xj - xi) * (lat - yi) / (yj - yi) + xi);
                if (intersects) inside = !inside;
            }
            return inside;
        }

        /// <summary>Average of the vertices — used as the polygon's map-centring point.</summary>
        private static (double Lat, double Lng) Centroid(List<GeoPointDTO> poly)
        {
            double lat = 0, lng = 0;
            foreach (var p in poly) { lat += p.Latitude; lng += p.Longitude; }
            return (lat / poly.Count, lng / poly.Count);
        }

        // Polygon is persisted as a compact JSON array of [lat, lng] pairs.
        private static string SerializePolygon(List<GeoPointDTO> poly) =>
            JsonSerializer.Serialize(poly.Select(p => new[] { p.Latitude, p.Longitude }));

        private static List<GeoPointDTO> DeserializePolygon(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                var raw = JsonSerializer.Deserialize<List<double[]>>(json);
                return raw?
                    .Where(p => p.Length >= 2)
                    .Select(p => new GeoPointDTO { Latitude = p[0], Longitude = p[1] })
                    .ToList() ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        private static BranchDTO ToDto(EMBranch b)
        {
            var polygon = DeserializePolygon(b.PolygonJson);
            return new()
            {
                Id = b.Id,
                Name = b.Name,
                Address = b.Address,
                Latitude = b.Latitude,
                Longitude = b.Longitude,
                RadiusMeters = b.RadiusMeters,
                GeofenceType = polygon.Count >= 3 ? "polygon" : "circle",
                Polygon = polygon,
                IsActive = b.IsActive,
                AssignedEmployees = b.Employees?
                    .Select(e => new EmployeeRefDTO { EmployeeId = e.Id, FullName = $"{e.FirstName} {e.LastName}" })
                    .OrderBy(e => e.FullName)
                    .ToList() ?? new(),
            };
        }
    }
}
