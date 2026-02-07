using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.RoleManagement.Interfaces
{
    /// <summary>
    /// Interface for role-based access control query operations providing comprehensive read-only access
    /// to role definitions and permission configurations. This repository interface defines contract methods
    /// for retrieving role information, permission sets, and role assignments essential for authorization
    /// decisions and access control management throughout the system.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Role definition retrieval by various identifiers (ID, role name)</para>
    /// <para>- Permission set inspection for specific roles and operations</para>
    /// <para>- Role assignment analysis across the user population</para>
    /// <para>- Active vs. inactive role filtering for administrative views</para>
    /// <para>- Paginated role listings for role management interfaces</para>
    /// <para>- Authorization decision support through permission checking</para>
    /// 
    /// <para>All query methods support the system's authorization infrastructure
    /// by providing efficient access to role definitions and permission configurations
    /// that drive access control decisions throughout the application.</para>
    /// </summary>
    public interface IRoleBasedAccessQueryRepository
    {
        /// <summary>
        /// Retrieves all role definitions from the system with complete permission configurations.
        /// Provides comprehensive role inventory for administrative management and system overview.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of role entities</returns>
        Task<IEnumerable<EMRoleBasedAccessControl>> GetAllRolesAsync();

        /// <summary>
        /// Retrieves a paginated list of role definitions for efficient display in administrative interfaces.
        /// Implements server-side pagination to optimize performance when managing extensive role catalogs.
        /// </summary>
        /// <param name="pageNumber">The current page number (1-indexed, must be greater than 0)</param>
        /// <param name="pageSize">The number of role records to display per page (1-100 range recommended)</param>
        /// <returns>A task with paginated role results containing the specified page of role definitions</returns>
        Task<IEnumerable<EMRoleBasedAccessControl>> GetRolesPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves detailed role information using the unique system-generated identifier.
        /// Provides complete role definition including all permission flags and descriptive metadata.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the role</param>
        /// <returns>A task containing the role entity if found; otherwise, null reference</returns>
        Task<EMRoleBasedAccessControl> GetRoleByIdAsync(int id);

        /// <summary>
        /// Retrieves role information using the unique role name for permission lookups.
        /// Essential for authorization middleware and permission verification workflows.
        /// </summary>
        /// <param name="roleName">The unique name identifying the role (case-insensitive comparison)</param>
        /// <returns>A task containing the role entity if found with matching role name; otherwise, null</returns>
        Task<EMRoleBasedAccessControl> GetRoleByNameAsync(string roleName);

        /// <summary>
        /// Retrieves all active role definitions currently enabled for assignment.
        /// Used for user management interfaces and role assignment dropdown controls.
        /// </summary>
        /// <returns>A task containing collection of active role entities</returns>
        Task<IEnumerable<EMRoleBasedAccessControl>> GetActiveRolesAsync();

        /// <summary>
        /// Verifies the existence of a role definition using the system identifier.
        /// Optimized for validation operations with minimal data transfer, only checking existence.
        /// </summary>
        /// <param name="id">The system identifier of the role to verify</param>
        /// <returns>True if a role with the specified ID exists; otherwise, false</returns>
        Task<bool> RoleExistsAsync(int id);

        /// <summary>
        /// Verifies the availability of a role name for new role creation.
        /// Prevents duplicate role names during role definition and modification processes.
        /// </summary>
        /// <param name="roleName">The role name to check for availability</param>
        /// <returns>True if the role name is available (not in use); otherwise, false</returns>
        Task<bool> IsRoleNameAvailableAsync(string roleName);

        /// <summary>
        /// Retrieves the total count of role definitions in the system for administrative dashboards.
        /// Provides quick access to role inventory metrics and system configuration overview.
        /// </summary>
        /// <returns>A task containing the total number of role definitions in the database</returns>
        Task<int> GetTotalRoleCountAsync();

        /// <summary>
        /// Retrieves all users assigned to a specific role for role utilization analysis.
        /// Enables administrators to understand role distribution and user assignment patterns.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role</param>
        /// <returns>A task containing collection of user entities assigned to the specified role</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetUsersInRoleAsync(int roleId);

        /// <summary>
        /// Retrieves the count of users currently assigned to each role for workload analysis.
        /// Provides role popularity metrics and supports capacity planning decisions.
        /// </summary>
        /// <returns>A task containing a dictionary mapping role names to their assigned user counts</returns>
        Task<Dictionary<string, int>> GetUserCountPerRoleAsync();

        /// <summary>
        /// Checks if a specific role has a particular permission enabled.
        /// Supports authorization decisions by verifying permission assignment to roles.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to check</param>
        /// <param name="permissionName">The name of the permission to verify (e.g., "CanViewEmployees", "CanManageUsers")</param>
        /// <returns>A task containing boolean indicating whether the role has the specified permission</returns>
        Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);

        /// <summary>
        /// Retrieves the complete permission set for a specific role as a dictionary.
        /// Provides comprehensive permission overview for role editing and detailed authorization views.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role</param>
        /// <returns>A task containing dictionary mapping permission names to their boolean enabled status</returns>
        Task<Dictionary<string, bool>> GetRolePermissionsAsync(int roleId);
    }
}
