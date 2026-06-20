using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Repository.RoleManagement.Interfaces;

namespace oamswlatifose.Server.Repository.RoleManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for role-based access control data retrieval operations.
    /// This repository provides comprehensive read-only access to role definitions, permission sets,
    /// and role-user assignments essential for authorization decisions throughout the system.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Role definition retrieval with complete permission configurations</para>
    /// <para>- Permission set inspection for authorization decisions</para>
    /// <para>- Role-user assignment analysis and reporting</para>
    /// <para>- Role availability and existence verification</para>
    /// <para>- Permission checking for specific roles</para>
    /// <para>- User count per role for administrative insights</para>
    /// </summary>
    public class RoleBasedAccessQueryRepository : IRoleBasedAccessQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleBasedAccessQueryRepository> _logger;

        public RoleBasedAccessQueryRepository(
            ApplicationDbContext context,
            ILogger<RoleBasedAccessQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMRoleBasedAccessControl>> GetAllRolesAsync()
        {
            _logger.LogDebug("Retrieving all role definitions");
            return await _context.EMRoleBasedAccessControls
                .Include(r => r.Users)
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMRoleBasedAccessControl>> GetRolesPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug("Retrieving roles page {PageNumber} with page size {PageSize}", pageNumber, pageSize);

            return await _context.EMRoleBasedAccessControls
                .Include(r => r.Users)
                .OrderBy(r => r.RoleName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<EMRoleBasedAccessControl> GetRoleByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving role with ID: {Id}", id);
            return await _context.EMRoleBasedAccessControls
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<EMRoleBasedAccessControl> GetRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name cannot be null or empty", nameof(roleName));

            _logger.LogDebug("Retrieving role with name: {RoleName}", roleName);
            return await _context.EMRoleBasedAccessControls
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
        }

        public async Task<IEnumerable<EMRoleBasedAccessControl>> GetActiveRolesAsync()
        {
            _logger.LogDebug("Retrieving active roles");
            return await _context.EMRoleBasedAccessControls
                .Include(r => r.Users)
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<bool> RoleExistsAsync(int id)
        {
            return await _context.EMRoleBasedAccessControls.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> IsRoleNameAvailableAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            return !await _context.EMRoleBasedAccessControls.AnyAsync(r => r.RoleName.ToLower() == roleName.ToLower());
        }

        public async Task<int> GetTotalRoleCountAsync()
        {
            return await _context.EMRoleBasedAccessControls.CountAsync();
        }

        public async Task<IEnumerable<EMAuthorizeruser>> GetUsersInRoleAsync(int roleId)
        {
            _logger.LogDebug("Retrieving users in role ID: {RoleId}", roleId);
            return await _context.EMAuthorizerusers
                .Include(u => u.Employee)
                .Where(u => u.RoleId == roleId && u.IsActive)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetUserCountPerRoleAsync()
        {
            _logger.LogDebug("Calculating user count per role");

            return await _context.EMRoleBasedAccessControls
                .Select(r => new { r.RoleName, UserCount = r.Users.Count(u => u.IsActive) })
                .ToDictionaryAsync(r => r.RoleName, r => r.UserCount);
        }

        public async Task<bool> RoleHasPermissionAsync(int roleId, string permissionName)
        {
            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return false;

            return permissionName switch
            {
                "CanViewEmployees" => role.CanViewEmployees,
                "CanEditEmployees" => role.CanEditEmployees,
                "CanDeleteEmployees" => role.CanDeleteEmployees,
                "CanViewAttendance" => role.CanViewAttendance,
                "CanEditAttendance" => role.CanEditAttendance,
                "CanGenerateReports" => role.CanGenerateReports,
                "CanManageUsers" => role.CanManageUsers,
                "CanManageRoles" => role.CanManageRoles,
                "CanAccessAdminPanel" => role.CanAccessAdminPanel,
                _ => false
            };
        }

        public async Task<Dictionary<string, bool>> GetRolePermissionsAsync(int roleId)
        {
            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
                return new Dictionary<string, bool>();

            return new Dictionary<string, bool>
            {
                ["CanViewEmployees"] = role.CanViewEmployees,
                ["CanEditEmployees"] = role.CanEditEmployees,
                ["CanDeleteEmployees"] = role.CanDeleteEmployees,
                ["CanViewAttendance"] = role.CanViewAttendance,
                ["CanEditAttendance"] = role.CanEditAttendance,
                ["CanGenerateReports"] = role.CanGenerateReports,
                ["CanManageUsers"] = role.CanManageUsers,
                ["CanManageRoles"] = role.CanManageRoles,
                ["CanAccessAdminPanel"] = role.CanAccessAdminPanel
            };
        }
    }   
}
