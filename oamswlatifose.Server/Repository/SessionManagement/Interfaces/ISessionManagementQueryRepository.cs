using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.SessionManagement.Interfaces
{
    /// <summary>
    /// Interface for user session query operations providing comprehensive read-only access to session information,
    /// user activity tracking, and concurrent session monitoring. This repository interface defines contract methods
    /// for retrieving session data essential for user activity auditing, security monitoring, and session management.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Active session retrieval for user activity monitoring</para>
    /// <para>- Session lookup by token for authentication validation</para>
    /// <para>- User session history with comprehensive activity timelines</para>
    /// <para>- Concurrent session analysis and session limit enforcement</para>
    /// <para>- Device and location tracking across user sessions</para>
    /// <para>- Session inactivity detection and expiration monitoring</para>
    /// 
    /// <para>All query methods support the session management infrastructure
    /// by providing efficient access to session data for authentication decisions,
    /// security monitoring, and user activity reporting.</para>
    /// </summary>
    public interface ISessionManagementQueryRepository
    {
        /// <summary>
        /// Retrieves all session records from the system including both active and historical sessions.
        /// Provides comprehensive session inventory for security auditing and administrative oversight.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of session entities</returns>
        Task<IEnumerable<EMSession>> GetAllSessionsAsync();

        /// <summary>
        /// Retrieves a specific session record using its unique system-generated identifier.
        /// Provides complete session details including login/logout times, device information, and location.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the session record</param>
        /// <returns>A task containing the session entity if found; otherwise, null reference</returns>
        Task<EMSession> GetSessionByIdAsync(int id);

        /// <summary>
        /// Retrieves a session record using the session token string for authentication validation.
        /// Essential for verifying active sessions during API request authorization and user activity tracking.
        /// </summary>
        /// <param name="sessionToken">The unique session token string to look up</param>
        /// <returns>A task containing the session entity if found with matching token; otherwise, null</returns>
        Task<EMSession> GetSessionByTokenAsync(string sessionToken);

        /// <summary>
        /// Retrieves all active, non-expired sessions for a specific user to monitor concurrent logins.
        /// Critical for enforcing maximum concurrent session policies and displaying active devices.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose active sessions to retrieve</param>
        /// <returns>A task containing collection of active, valid session entities for the specified user</returns>
        Task<IEnumerable<EMSession>> GetActiveSessionsByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves complete session history for a specific user including expired and terminated sessions.
        /// Provides comprehensive user activity timeline for security auditing and behavior analysis.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose session history to retrieve</param>
        /// <returns>A task containing collection of all session entities associated with the specified user</returns>
        Task<IEnumerable<EMSession>> GetSessionHistoryByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves all sessions that have expired but have not been marked as inactive.
        /// Supports background cleanup jobs and maintenance operations for session table management.
        /// </summary>
        /// <returns>A task containing collection of expired session entities requiring cleanup</returns>
        Task<IEnumerable<EMSession>> GetExpiredSessionsAsync();

        /// <summary>
        /// Retrieves all sessions with no recent activity beyond the specified threshold.
        /// Enables identification of idle sessions for timeout enforcement and resource optimization.
        /// </summary>
        /// <param name="inactivityThreshold">The time threshold for considering a session inactive</param>
        /// <returns>A task containing collection of idle session entities exceeding the inactivity threshold</returns>
        Task<IEnumerable<EMSession>> GetInactiveSessionsAsync(TimeSpan inactivityThreshold);

        /// <summary>
        /// Retrieves session records filtered by specific device type (Mobile, Desktop, Tablet, etc.).
        /// Enables device-based session analysis and security monitoring for unusual device patterns.
        /// </summary>
        /// <param name="deviceType">The device type to filter sessions by</param>
        /// <returns>A task containing collection of session entities associated with the specified device type</returns>
        Task<IEnumerable<EMSession>> GetSessionsByDeviceTypeAsync(string deviceType);

        /// <summary>
        /// Retrieves session records originating from a specific geographic location.
        /// Supports location-based security monitoring and travel pattern analysis.
        /// </summary>
        /// <param name="location">The location string to filter sessions by</param>
        /// <returns>A task containing collection of session entities associated with the specified location</returns>
        Task<IEnumerable<EMSession>> GetSessionsByLocationAsync(string location);

        /// <summary>
        /// Verifies the validity and active status of a session token for authentication decisions.
        /// Checks expiration, active flag, and ensures session exists in the system.
        /// </summary>
        /// <param name="sessionToken">The session token string to validate</param>
        /// <returns>A task containing boolean indicating whether the session is valid and active</returns>
        Task<bool> IsSessionValidAsync(string sessionToken);

        /// <summary>
        /// Retrieves the count of concurrent active sessions for each user for license compliance.
        /// Enables enforcement of maximum concurrent session limits based on user subscription tiers.
        /// </summary>
        /// <returns>A task containing dictionary mapping user IDs to their active session counts</returns>
        Task<Dictionary<int, int>> GetActiveSessionCountPerUserAsync();

        /// <summary>
        /// Retrieves the total count of active sessions in the system for real-time monitoring.
        /// Provides system load metrics and supports capacity planning decisions.
        /// </summary>
        /// <returns>A task containing the total number of active, non-expired sessions</returns>
        Task<int> GetActiveSessionCountAsync();
    }

}
