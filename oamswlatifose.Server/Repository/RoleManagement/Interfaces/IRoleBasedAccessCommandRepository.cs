using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.RoleManagement.Interfaces
{
    /// <summary>
    /// Interface for role-based access control data modification operations defining contracts for all create,
    /// update, delete, and assignment operations on role definitions. This repository interface establishes
    /// the pattern for role management with comprehensive permission configuration and authorization governance.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Role definition creation with complete permission configuration</para>
    /// <para>- Permission set modification with granular flag updates</para>
    /// <para>- Role deletion with assignment validation safeguards</para>
    /// <para>- Role activation and deactivation with system role protection</para>
    /// <para>- Role cloning for efficient permission template creation</para>
    /// <para>- User role assignment for authorization implementation</para>
    /// 
    /// <para>All methods enforce security policies, prevent unauthorized privilege escalation,
    /// and maintain comprehensive audit trails of all role configuration changes.</para>
    /// </summary>
    public interface IRoleBasedAccessCommandRepository
    {
        /// <summary>
        /// Creates a new role definition with comprehensive permission configuration.
        /// Performs duplicate role name validation and initializes the role with default active status.
        /// </summary>
        /// <param name="role">The role entity containing role name, description, and all permission flags</param>
        /// <returns>A task representing the asynchronous operation with the newly created role entity</returns>
        Task<EMRoleBasedAccessControl> CreateRoleAsync(EMRoleBasedAccessControl role);

        /// <summary>
        /// Updates an existing role definition with modified permission configuration.
        /// Validates role name uniqueness if changed and maintains audit trail through timestamp updates.
        /// </summary>
        /// <param name="role">The role entity containing updated property values</param>
        /// <returns>A task representing the asynchronous operation with the fully updated role entity</returns>
        Task<EMRoleBasedAccessControl> UpdateRoleAsync(EMRoleBasedAccessControl role);

        /// <summary>
        /// Permanently removes a role definition from the system with validation.
        /// Prevents deletion of roles that are currently assigned to active users to maintain referential integrity.
        /// </summary>
        /// <param name="id">The unique system identifier of the role to delete</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> DeleteRoleAsync(int id);

        /// <summary>
        /// Updates the active status of a role to enable or disable assignment availability.
        /// Prevents deactivation of critical system roles that are essential for application functionality.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to update</param>
        /// <param name="isActive">The desired active state (true for active, false for inactive)</param>
        /// <returns>A task representing the asynchronous operation with the updated role entity</returns>
        Task<EMRoleBasedAccessControl> SetRoleActiveStatusAsync(int roleId, bool isActive);

        /// <summary>
        /// Updates specific permissions for a role using a dictionary of permission flags.
        /// Enables granular permission configuration without requiring full role entity transfer.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role to update</param>
        /// <param name="permissions">Dictionary mapping permission property names to their desired boolean values</param>
        /// <returns>A task representing the asynchronous operation with the updated role entity</returns>
        Task<EMRoleBasedAccessControl> UpdateRolePermissionsAsync(int roleId, Dictionary<string, bool> permissions);

        /// <summary>
        /// Creates a duplicate of an existing role with a new name and optionally copies all permission settings.
        /// Useful for creating role variants based on existing configurations without manual permission reconfiguration.
        /// </summary>
        /// <param name="sourceRoleId">The unique identifier of the role to copy from</param>
        /// <param name="newRoleName">The name for the new role being created</param>
        /// <param name="description">The description for the new role</param>
        /// <returns>A task representing the asynchronous operation with the newly created role entity</returns>
        Task<EMRoleBasedAccessControl> CloneRoleAsync(int sourceRoleId, string newRoleName, string description);

        /// <summary>
        /// Assigns a role to a specific user account for authorization purposes.
        /// Updates the user's role association and maintains audit trail of the assignment.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to assign the role to</param>
        /// <param name="roleId">The unique identifier of the role to assign</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> AssignRoleToUserAsync(int userId, int roleId);
    }
}
