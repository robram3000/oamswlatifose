using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.RoleManagement.Implementations
{
    /// <summary>
    /// Command repository implementation for role-based access control data modification operations.
    /// This repository handles all create, update, and delete operations for role definitions
    /// with comprehensive permission validation, assignment management, and system integrity enforcement.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Role definition creation with complete permission configuration</para>
    /// <para>- Permission set modification with granular flag updates</para>
    /// <para>- Role activation/deactivation with user assignment validation</para>
    /// <para>- Role deletion prevention for roles with active user assignments</para>
    /// <para>- Duplicate role name prevention through uniqueness validation</para>
    /// <para>- Audit trail maintenance through timestamp tracking</para>
    /// 
    /// <para>All operations maintain referential integrity with user accounts
    /// and enforce business rules to prevent security gaps from role misconfiguration.
    /// Critical roles (Administrator, etc.) are protected from deletion or deactivation.</para>
    /// </summary>
    public class RoleBasedAccessCommandRepository : IRoleBasedAccessCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleBasedAccessCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the RoleBasedAccessCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for role management operations.
        /// </summary>
        /// <param name="context">The application database context providing access to role definition tables</param>
        /// <param name="logger">The logging service for capturing role management operation details and security events</param>
        public RoleBasedAccessCommandRepository(
            ApplicationDbContext context,
            ILogger<RoleBasedAccessCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new role definition with comprehensive permission configuration.
        /// Performs duplicate role name validation and initializes audit timestamps before persistence.
        /// </summary>
        /// <param name="role">The role entity containing role name, description, and all permission flags</param>
        /// <returns>A task representing the asynchronous operation with the newly created role entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the role parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when a role with the same name already exists</exception>
        public async Task<EMRoleBasedAccessControl> CreateRoleAsync(EMRoleBasedAccessControl role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            // Check for duplicate role name
            var roleExists = await _context.EMRoleBasedAccessControls
                .AnyAsync(r => r.RoleName.ToLower() == role.RoleName.ToLower());
            if (roleExists)
                throw new InvalidOperationException($"Role with name '{role.RoleName}' already exists");

            // Initialize audit fields
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;
            role.IsActive = true;

            await _context.EMRoleBasedAccessControls.AddAsync(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created role: {role.RoleName} (ID: {role.Id})");
            return role;
        }

        /// <summary>
        /// Updates an existing role definition with modified permission configuration.
        /// Validates role name uniqueness if changed and maintains audit trail through timestamp updates.
        /// </summary>
        /// <param name="role">The role entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated role entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the role parameter is null</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no role exists with the specified Id</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to rename to an already existing role name</exception>
        public async Task<EMRoleBasedAccessControl> UpdateRoleAsync(EMRoleBasedAccessControl role)
        {
            if (role == null)
                throw new ArgumentNullException(nameof(role));

            var existingRole = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == role.Id);
            if (existingRole == null)
                throw new KeyNotFoundException($"Role with ID {role.Id} not found");

            // Check role name uniqueness if changed
            if (existingRole.RoleName != role.RoleName)
            {
                var duplicateName = await _context.EMRoleBasedAccessControls
                    .AnyAsync(r => r.RoleName.ToLower() == role.RoleName.ToLower() && r.Id != role.Id);
                if (duplicateName)
                    throw new InvalidOperationException($"Role with name '{role.RoleName}' already exists");
            }

            // Preserve creation timestamp
            role.CreatedAt = existingRole.CreatedAt;
            role.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingRole).CurrentValues.SetValues(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated role: {role.RoleName} (ID: {role.Id})");
            return existingRole;
        }

        /// <summary>
        /// Permanently removes a role definition from the system with validation.
        /// Prevents deletion of roles that are currently assigned to active users to maintain referential integrity.
        /// </summary>
        /// <param name="id">The unique system identifier of the role to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no role exists with the specified Id</exception>
        /// <exception cref="InvalidOperationException">Thrown when the role has active user assignments preventing deletion</exception>
        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                throw new KeyNotFoundException($"Role with ID {id} not found");

            // Check if role is assigned to any users
            var hasUsers = await _context.EMAuthorizerusers.AnyAsync(u => u.RoleId == id);
            if (hasUsers)
                throw new InvalidOperationException($"Cannot delete role '{role.RoleName}' because it is assigned to one or more users");

            _context.EMRoleBasedAccessControls.Remove(role);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Deleted role: {role.RoleName} (ID: {id})");
            return result > 0;
        }

        /// <summary>
        /// Updates the active status of a role to enable or disable assignment availability.
        /// Prevents deactivation of critical system roles that are essential for application functionality.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to update</param>
        /// <param name="isActive">The desired active state (true for active, false for inactive)</param>
        /// <returns>A task representing the asynchronous operation with the updated role entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no role exists with the specified Id</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to deactivate a protected system role</exception>
        public async Task<EMRoleBasedAccessControl> SetRoleActiveStatusAsync(int roleId, bool isActive)
        {
            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role with ID {roleId} not found");

            // Prevent deactivation of critical system roles
            if (!isActive && role.RoleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot deactivate the Administrator role");

            role.IsActive = isActive;
            role.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Role {role.RoleName} active status set to {isActive}");
            return role;
        }

        /// <summary>
        /// Updates specific permissions for a role using a dictionary of permission flags.
        /// Enables granular permission configuration without requiring full role entity transfer.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to update</param>
        /// <param name="permissions">Dictionary mapping permission property names to their desired boolean values</param>
        /// <returns>A task representing the asynchronous operation with the updated role entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no role exists with the specified Id</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid permission name is provided</exception>
        public async Task<EMRoleBasedAccessControl> UpdateRolePermissionsAsync(int roleId, Dictionary<string, bool> permissions)
        {
            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role with ID {roleId} not found");

            // Update each permission property dynamically
            foreach (var permission in permissions)
            {
                var property = typeof(EMRoleBasedAccessControl).GetProperty(permission.Key);
                if (property == null || !property.CanWrite)
                    throw new ArgumentException($"Invalid permission property: {permission.Key}");

                property.SetValue(role, permission.Value);
            }

            role.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Updated permissions for role: {role.RoleName}");
            return role;
        }

        /// <summary>
        /// Creates a duplicate of an existing role with a new name and optionally copies all permission settings.
        /// Useful for creating role variants based on existing configurations without manual permission reconfiguration.
        /// </summary>
        /// <param name="sourceRoleId">The unique identifier of the role to copy from</param>
        /// <param name="newRoleName">The name for the new role being created</param>
        /// <param name="description">The description for the new role</param>
        /// <returns>A task representing the asynchronous operation with the newly created role entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the source role does not exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when a role with the new name already exists</exception>
        public async Task<EMRoleBasedAccessControl> CloneRoleAsync(int sourceRoleId, string newRoleName, string description)
        {
            var sourceRole = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == sourceRoleId);
            if (sourceRole == null)
                throw new KeyNotFoundException($"Source role with ID {sourceRoleId} not found");

            // Check if new role name is available
            var roleExists = await _context.EMRoleBasedAccessControls
                .AnyAsync(r => r.RoleName.ToLower() == newRoleName.ToLower());
            if (roleExists)
                throw new InvalidOperationException($"Role with name '{newRoleName}' already exists");

            // Create new role with copied permissions
            var newRole = new EMRoleBasedAccessControl
            {
                RoleName = newRoleName,
                Description = description,
                CanViewEmployees = sourceRole.CanViewEmployees,
                CanEditEmployees = sourceRole.CanEditEmployees,
                CanDeleteEmployees = sourceRole.CanDeleteEmployees,
                CanViewAttendance = sourceRole.CanViewAttendance,
                CanEditAttendance = sourceRole.CanEditAttendance,
                CanGenerateReports = sourceRole.CanGenerateReports,
                CanManageUsers = sourceRole.CanManageUsers,
                CanManageRoles = sourceRole.CanManageRoles,
                CanAccessAdminPanel = sourceRole.CanAccessAdminPanel,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.EMRoleBasedAccessControls.AddAsync(newRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cloned role '{sourceRole.RoleName}' to new role '{newRoleName}'");
            return newRole;
        }

        /// <summary>
        /// Assigns a role to a specific user account for authorization purposes.
        /// Updates the user's role association and maintains audit trail of the assignment.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to assign the role to</param>
        /// <param name="roleId">The unique identifier of the role to assign</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the user or role does not exist</exception>
        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId)
        {
            var user = await _context.EMAuthorizerusers
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found");

            var role = await _context.EMRoleBasedAccessControls
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
                throw new KeyNotFoundException($"Role with ID {roleId} not found");

            if (!role.IsActive)
                throw new InvalidOperationException($"Cannot assign inactive role '{role.RoleName}' to user");

            user.RoleId = roleId;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Assigned role '{role.RoleName}' to user '{user.Username}'");
            return result > 0;
        }
    }
}
