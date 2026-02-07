using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.UserManagement.Interfaces
{
    /// <summary>
    /// Interface for user account query operations providing comprehensive read-only access to system user information.
    /// This repository interface defines contract methods for retrieving user account details, authentication data,
    /// and security-related user information essential for identity management and access control systems.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- User authentication and verification operations (username, email, ID lookups)</para>
    /// <para>- Account status and lockout state monitoring</para>
    /// <para>- Role-based user filtering for authorization decisions</para>
    /// <para>- User existence verification for validation workflows</para>
    /// <para>- Comprehensive user profile retrieval with related role and employee data</para>
    /// <para>- Paginated user listings for administrative interfaces</para>
    /// 
    /// <para>All query methods are optimized for authentication performance
    /// and implement secure data access patterns to protect sensitive user information.</para>
    /// </summary>
    public interface IUserAccountQueryRepository
    {
        /// <summary>
        /// Retrieves all system user accounts with complete profile information including role assignments.
        /// Provides comprehensive user list for administrative management and system overview functions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of user entities</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetAllUsersAsync();

        /// <summary>
        /// Retrieves a paginated list of user accounts for efficient display in administrative interfaces.
        /// Implements server-side pagination to optimize performance when managing large user bases.
        /// </summary>
        /// <param name="pageNumber">The current page number (1-indexed, must be greater than 0)</param>
        /// <param name="pageSize">The number of user records to display per page (1-100 range recommended)</param>
        /// <returns>A task with paginated user results containing the specified page of user records</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetUsersPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves detailed user account information using the unique system-generated identifier.
        /// Provides complete user profile including role, employee association, and security settings.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the user account</param>
        /// <returns>A task containing the user entity if found; otherwise, null reference</returns>
        Task<EMAuthorizeruser> GetUserByIdAsync(int id);

        /// <summary>
        /// Retrieves user account information using the unique username for authentication flows.
        /// Critical for login verification, identity validation, and user lookup operations.
        /// </summary>
        /// <param name="username">The unique username associated with the user account (case-insensitive comparison)</param>
        /// <returns>A task containing the user entity if found with matching username; otherwise, null</returns>
        Task<EMAuthorizeruser> GetUserByUsernameAsync(string username);

        /// <summary>
        /// Retrieves user account information using the registered email address.
        /// Essential for password recovery workflows, email verification, and communication systems.
        /// </summary>
        /// <param name="email">The email address associated with the user account (case-insensitive comparison)</param>
        /// <returns>A task containing the user entity if found with matching email; otherwise, null</returns>
        Task<EMAuthorizeruser> GetUserByEmailAsync(string email);

        /// <summary>
        /// Retrieves all user accounts associated with a specific role for role-based permissions management.
        /// Enables analysis of role assignments and supports authorization decision workflows.
        /// </summary>
        /// <param name="roleId">The unique identifier of the role</param>
        /// <returns>A task containing collection of user entities assigned to the specified role</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetUsersByRoleIdAsync(int roleId);

        /// <summary>
        /// Retrieves the user account associated with a specific employee record.
        /// Supports employee-user linking verification and integrated employee portal access.
        /// </summary>
        /// <param name="employeeId">The unique identifier of the employee</param>
        /// <returns>A task containing the user entity if found with associated employee; otherwise, null</returns>
        Task<EMAuthorizeruser> GetUserByEmployeeIdAsync(int employeeId);

        /// <summary>
        /// Verifies the existence of a user account using the system identifier.
        /// Optimized for validation operations with minimal data transfer, only checking existence.
        /// </summary>
        /// <param name="id">The system identifier of the user to verify</param>
        /// <returns>True if a user account with the specified ID exists; otherwise, false</returns>
        Task<bool> UserExistsAsync(int id);

        /// <summary>
        /// Verifies the availability of a username for new account creation.
        /// Prevents duplicate usernames during registration and account modification processes.
        /// </summary>
        /// <param name="username">The username to check for availability</param>
        /// <returns>True if the username is available (not in use); otherwise, false</returns>
        Task<bool> IsUsernameAvailableAsync(string username);

        /// <summary>
        /// Verifies the availability of an email address for new account creation.
        /// Ensures email uniqueness across the system for communication and identification purposes.
        /// </summary>
        /// <param name="email">The email address to check for availability</param>
        /// <returns>True if the email is available (not in use); otherwise, false</returns>
        Task<bool> IsEmailAvailableAsync(string email);

        /// <summary>
        /// Retrieves all active user accounts currently enabled for system access.
        /// Used for active user reporting, license counting, and system utilization analysis.
        /// </summary>
        /// <returns>A task containing collection of active user entities</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetActiveUsersAsync();

        /// <summary>
        /// Retrieves all locked-out user accounts that are temporarily disabled due to failed authentication attempts.
        /// Supports security monitoring, account recovery workflows, and administrative oversight.
        /// </summary>
        /// <returns>A task containing collection of locked-out user entities</returns>
        Task<IEnumerable<EMAuthorizeruser>> GetLockedOutUsersAsync();

        /// <summary>
        /// Retrieves the total count of user accounts in the system for administrative dashboards.
        /// Provides quick access to user population metrics and system scaling information.
        /// </summary>
        /// <returns>A task containing the total number of user accounts in the database</returns>
        Task<int> GetTotalUserCountAsync();

        /// <summary>
        /// Retrieves a user account with complete navigation properties including role permissions and associated employee.
        /// Provides comprehensive user profile for detailed views and complex authorization decisions.
        /// </summary>
        /// <param name="id">The unique system identifier of the user account</param>
        /// <returns>A task containing the user entity with loaded navigation properties if found; otherwise, null</returns>
        Task<EMAuthorizeruser> GetUserWithDetailsAsync(int id);
    }
}
