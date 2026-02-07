using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.AuditManagement.Interfaces
{
    /// <summary>
    /// Interface for authentication audit log query operations providing comprehensive read-only access
    /// to security event information, user authentication history, and compliance reporting data.
    /// This repository interface defines contract methods for retrieving authentication log entries
    /// essential for security monitoring, incident investigation, and regulatory compliance.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Authentication event retrieval with comprehensive filtering (success, failure, all)</para>
    /// <para>- User-specific authentication history for account activity analysis</para>
    /// <para>- Time-based log analysis for security pattern detection</para>
    /// <para>- IP address and location-based event correlation</para>
    /// <para>- Failed attempt analysis for brute force detection</para>
    /// <para>- Compliance reporting with date-range queries</para>
    /// <para>- Device fingerprinting and user agent analysis</para>
    /// 
    /// <para>All query methods support security auditing requirements by providing
    /// efficient access to authentication events with comprehensive filtering and
    /// aggregation capabilities essential for security operations and compliance reporting.</para>
    /// </summary>
    public interface IAuthenticationAuditQueryRepository
    {
        /// <summary>
        /// Retrieves all authentication log entries from the system for comprehensive security auditing.
        /// Provides complete audit trail of all authentication events across the entire system lifetime.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of authentication log entities</returns>
        Task<IEnumerable<EMAuthLog>> GetAllAuthLogsAsync();

        /// <summary>
        /// Retrieves a specific authentication log entry using its unique system-generated identifier.
        /// Provides complete event details including timestamp, user, action, and outcome information.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the authentication log entry</param>
        /// <returns>A task containing the authentication log entity if found; otherwise, null reference</returns>
        Task<EMAuthLog> GetAuthLogByIdAsync(int id);

        /// <summary>
        /// Retrieves a paginated list of authentication log entries for efficient display in security dashboards.
        /// Implements server-side pagination to optimize performance when reviewing extensive audit histories.
        /// </summary>
        /// <param name="pageNumber">The current page number (1-indexed, must be greater than 0)</param>
        /// <param name="pageSize">The number of log entries to display per page (1-100 range recommended)</param>
        /// <returns>A task with paginated authentication log results containing the specified page of entries</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves complete authentication history for a specific user account.
        /// Essential for user activity monitoring, account compromise investigation, and security audits.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose authentication history to retrieve</param>
        /// <returns>A task containing collection of authentication log entries associated with the specified user</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves authentication history for a specific username, including attempts before account creation.
        /// Critical for investigating suspicious activity targeting specific usernames regardless of account existence.
        /// </summary>
        /// <param name="username">The username attempted during authentication events</param>
        /// <returns>A task containing collection of authentication log entries associated with the specified username</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByUsernameAsync(string username);

        /// <summary>
        /// Retrieves all successful authentication events for access analysis and user activity reporting.
        /// Enables tracking of legitimate system access patterns and user login behavior.
        /// </summary>
        /// <returns>A task containing collection of successful authentication log entries</returns>
        Task<IEnumerable<EMAuthLog>> GetSuccessfulAuthLogsAsync();

        /// <summary>
        /// Retrieves all failed authentication events for security monitoring and brute force detection.
        /// Critical for identifying potential attacks, compromised credentials, and authentication issues.
        /// </summary>
        /// <returns>A task containing collection of failed authentication log entries</returns>
        Task<IEnumerable<EMAuthLog>> GetFailedAuthLogsAsync();

        /// <summary>
        /// Retrieves authentication events within a specified date range for compliance reporting.
        /// Supports regulatory audit requirements and security investigations over specific time periods.
        /// </summary>
        /// <param name="startDate">The beginning date of the audit period (inclusive)</param>
        /// <param name="endDate">The ending date of the audit period (inclusive)</param>
        /// <returns>A task containing collection of authentication log entries within the specified date range</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves authentication events filtered by specific action type (Login, Logout, PasswordChange, etc.).
        /// Enables targeted analysis of specific authentication workflows and security events.
        /// </summary>
        /// <param name="action">The authentication action type to filter logs by</param>
        /// <returns>A task containing collection of authentication log entries with the specified action</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByActionAsync(string action);

        /// <summary>
        /// Retrieves authentication events originating from a specific IP address for threat investigation.
        /// Essential for identifying suspicious activity patterns and potential attack sources.
        /// </summary>
        /// <param name="ipAddress">The IP address to filter authentication logs by</param>
        /// <returns>A task containing collection of authentication log entries associated with the specified IP address</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByIPAddressAsync(string ipAddress);

        /// <summary>
        /// Retrieves authentication events associated with a specific device type for security profiling.
        /// Enables analysis of device-based access patterns and identification of unusual device usage.
        /// </summary>
        /// <param name="deviceType">The device type to filter authentication logs by</param>
        /// <returns>A task containing collection of authentication log entries associated with the specified device type</returns>
        Task<IEnumerable<EMAuthLog>> GetAuthLogsByDeviceTypeAsync(string deviceType);

        /// <summary>
        /// Retrieves the count of failed authentication attempts grouped by IP address for threat detection.
        /// Identifies potential brute force attacks and suspicious activity sources for blocking.
        /// </summary>
        /// <param name="since">The start date for analyzing failed attempt patterns</param>
        /// <returns>A task containing dictionary mapping IP addresses to their failed attempt counts</returns>
        Task<Dictionary<string, int>> GetFailedAttemptCountByIPAddressAsync(DateTime since);

        /// <summary>
        /// Retrieves the count of failed authentication attempts grouped by username for account monitoring.
        /// Identifies accounts under active attack and supports proactive account protection measures.
        /// </summary>
        /// <param name="since">The start date for analyzing failed attempt patterns</param>
        /// <returns>A task containing dictionary mapping usernames to their failed attempt counts</returns>
        Task<Dictionary<string, int>> GetFailedAttemptCountByUsernameAsync(DateTime since);

        /// <summary>
        /// Retrieves the total count of authentication events within a specified time period.
        /// Provides system activity metrics and supports capacity planning for security monitoring.
        /// </summary>
        /// <param name="startDate">The beginning date of the counting period (inclusive)</param>
        /// <param name="endDate">The ending date of the counting period (inclusive)</param>
        /// <returns>A task containing the total number of authentication events in the specified period</returns>
        Task<int> GetAuthEventCountByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
