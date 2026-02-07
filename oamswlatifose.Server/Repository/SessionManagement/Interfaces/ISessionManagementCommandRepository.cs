using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.SessionManagement.Interfaces
{
    /// <summary>
    /// Interface for user session data modification operations defining contracts for all create,
    /// update, delete, and termination operations on session entities. This repository interface
    /// establishes the pattern for session lifecycle management with comprehensive activity tracking.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Session creation with unique token generation for authenticated users</para>
    /// <para>- Individual session termination for logout operations</para>
    /// <para>- Bulk session termination for security incidents and account changes</para>
    /// <para>- Session activity tracking for timeout enforcement</para>
    /// <para>- Session expiration extension for persistent login scenarios</para>
    /// <para>- Automated cleanup of expired sessions for database maintenance</para>
    /// 
    /// <para>All methods enforce session timeout policies, maintain accurate
    /// activity tracking, and support concurrent session limit enforcement.</para>
    /// </summary>
    public interface ISessionManagementCommandRepository
    {
        /// <summary>
        /// Creates a new user session upon successful authentication.
        /// Generates a unique session token and records login metadata including device and location information.
        /// </summary>
        /// <param name="session">The session entity containing user ID, token, IP address, and other session metadata</param>
        /// <returns>A task representing the asynchronous operation with the newly created session entity</returns>
        Task<EMSession> CreateSessionAsync(EMSession session);

        /// <summary>
        /// Terminates an active user session upon logout or administrative intervention.
        /// Records the logout timestamp and marks the session as inactive.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to terminate</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> TerminateSessionAsync(int sessionId);

        /// <summary>
        /// Terminates all active sessions for a specific user, effectively logging them out from all devices.
        /// Used during password changes, security incidents, or when user account is deactivated.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose sessions should be terminated</param>
        /// <returns>A task representing the asynchronous operation with count of terminated sessions</returns>
        Task<int> TerminateAllUserSessionsAsync(int userId);

        /// <summary>
        /// Updates the last activity timestamp for an active session to track user engagement.
        /// Essential for session timeout enforcement and inactivity monitoring.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to update</param>
        /// <returns>A task representing the asynchronous operation with the updated session entity</returns>
        Task<EMSession> UpdateSessionActivityAsync(int sessionId);

        /// <summary>
        /// Permanently removes expired and terminated sessions from the database for maintenance.
        /// Implements configurable retention policy to balance audit requirements with database size.
        /// </summary>
        /// <param name="retentionThreshold">Sessions terminated or expired before this date will be deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted sessions</returns>
        Task<int> CleanupExpiredSessionsAsync(DateTime retentionThreshold);

        /// <summary>
        /// Extends the expiration time for an active session, typically used for "remember me" functionality.
        /// Allows longer session duration for trusted devices while maintaining security controls.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to extend</param>
        /// <param name="extensionTime">The additional time to add to the session expiration</param>
        /// <returns>A task representing the asynchronous operation with the updated session entity</returns>
        Task<EMSession> ExtendSessionExpirationAsync(int sessionId, TimeSpan extensionTime);
    }
}
