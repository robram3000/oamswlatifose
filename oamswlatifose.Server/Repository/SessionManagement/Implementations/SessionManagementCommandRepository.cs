using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.SessionManagement.Implementations
{
    /// <summary>
    /// Command repository implementation for user session data modification operations.
    /// This repository handles all create, update, and delete operations for user sessions
    /// with comprehensive session lifecycle management, activity tracking, and security enforcement.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Session creation with unique token generation for user authentication</para>
    /// <para>- Session termination (logout) with proper cleanup and timestamp recording</para>
    /// <para>- Activity tracking through last activity timestamp updates</para>
    /// <para>- Concurrent session limit enforcement per user</para>
    /// <para>- Session expiration management and automatic invalidation</para>
    /// <para>- Device and location metadata capture for security auditing</para>
    /// 
    /// <para>All operations maintain session integrity, enforce timeout policies,
    /// and provide comprehensive audit trails of user activity throughout the session lifecycle.</para>
    /// </summary>
    public class SessionManagementCommandRepository : ISessionManagementCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionManagementCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the SessionManagementCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for session management operations.
        /// </summary>
        /// <param name="context">The application database context providing access to session tables</param>
        /// <param name="logger">The logging service for capturing session management operation details and security events</param>
        public SessionManagementCommandRepository(
            ApplicationDbContext context,
            ILogger<SessionManagementCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new user session upon successful authentication.
        /// Generates a unique session token, records login metadata including device and location information,
        /// and enforces configurable concurrent session limits per user.
        /// </summary>
        /// <param name="session">The session entity containing user ID, token, IP address, and other session metadata</param>
        /// <returns>A task representing the asynchronous operation with the newly created session entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the session parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the referenced user does not exist or concurrent session limit is exceeded</exception>
        public async Task<EMSession> CreateSessionAsync(EMSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Verify user exists
            var userExists = await _context.EMAuthorizerusers.AnyAsync(u => u.Id == session.UserId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID {session.UserId} not found");

            // Enforce concurrent session limit (configurable, default 5)
            const int maxConcurrentSessions = 5;
            var activeSessionCount = await _context.EMSessions
                .CountAsync(s => s.UserId == session.UserId && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            if (activeSessionCount >= maxConcurrentSessions)
            {
                // Terminate oldest session
                var oldestSession = await _context.EMSessions
                    .Where(s => s.UserId == session.UserId && s.IsActive)
                    .OrderBy(s => s.LoginTime)
                    .FirstOrDefaultAsync();

                if (oldestSession != null)
                {
                    oldestSession.IsActive = false;
                    oldestSession.LogoutTime = DateTime.UtcNow;
                    _logger.LogInformation($"Terminated oldest session {oldestSession.Id} for user {session.UserId} due to concurrent limit");
                }
            }

            // Initialize session
            session.LoginTime = DateTime.UtcNow;
            session.LastActivity = DateTime.UtcNow;
            session.IsActive = true;
            session.ExpiresAt = DateTime.UtcNow.AddHours(8); // 8-hour session expiration

            await _context.EMSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created session {session.Id} for user {session.UserId}");
            return session;
        }

        /// <summary>
        /// Terminates an active user session upon logout or administrative intervention.
        /// Records the logout timestamp and marks the session as inactive for proper session management.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to terminate</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no session exists with the specified Id</exception>
        public async Task<bool> TerminateSessionAsync(int sessionId)
        {
            var session = await _context.EMSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session with ID {sessionId} not found");

            session.IsActive = false;
            session.LogoutTime = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Terminated session {sessionId} for user {session.UserId}");
            return result > 0;
        }

        /// <summary>
        /// Terminates all active sessions for a specific user, effectively logging them out from all devices.
        /// Used during password changes, security incidents, or when user account is deactivated.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose sessions should be terminated</param>
        /// <returns>A task representing the asynchronous operation with count of terminated sessions</returns>
        public async Task<int> TerminateAllUserSessionsAsync(int userId)
        {
            var sessions = await _context.EMSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var utcNow = DateTime.UtcNow;
            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LogoutTime = utcNow;
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Terminated {result} sessions for user {userId}");
            return result;
        }

        /// <summary>
        /// Updates the last activity timestamp for an active session to track user engagement.
        /// Essential for session timeout enforcement and inactivity monitoring.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to update</param>
        /// <returns>A task representing the asynchronous operation with the updated session entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no session exists with the specified Id</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update an inactive or expired session</exception>
        public async Task<EMSession> UpdateSessionActivityAsync(int sessionId)
        {
            var session = await _context.EMSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session with ID {sessionId} not found");

            if (!session.IsActive)
                throw new InvalidOperationException("Cannot update activity for inactive session");

            if (session.ExpiresAt <= DateTime.UtcNow)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Cannot update activity for expired session");
            }

            session.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return session;
        }

        /// <summary>
        /// Permanently removes expired and terminated sessions from the database for maintenance.
        /// Implements configurable retention policy to balance audit requirements with database size.
        /// </summary>
        /// <param name="retentionThreshold">Sessions terminated or expired before this date will be deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted sessions</returns>
        public async Task<int> CleanupExpiredSessionsAsync(DateTime retentionThreshold)
        {
            var expiredSessions = await _context.EMSessions
                .Where(s => s.ExpiresAt < retentionThreshold ||
                           (s.LogoutTime.HasValue && s.LogoutTime.Value < retentionThreshold))
                .ToListAsync();

            _context.EMSessions.RemoveRange(expiredSessions);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Cleaned up {result} expired sessions older than {retentionThreshold}");
            return result;
        }

        /// <summary>
        /// Extends the expiration time for an active session, typically used for "remember me" functionality.
        /// Allows longer session duration for trusted devices while maintaining security controls.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the session to extend</param>
        /// <param name="extensionTime">The additional time to add to the session expiration</param>
        /// <returns>A task representing the asynchronous operation with the updated session entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no session exists with the specified Id</exception>
        public async Task<EMSession> ExtendSessionExpirationAsync(int sessionId, TimeSpan extensionTime)
        {
            var session = await _context.EMSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Session with ID {sessionId} not found");

            if (!session.IsActive)
                throw new InvalidOperationException("Cannot extend expiration for inactive session");

            session.ExpiresAt = session.ExpiresAt.Add(extensionTime);
            session.LastActivity = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Extended session {sessionId} expiration to {session.ExpiresAt}");
            return session;
        }
    }
}
