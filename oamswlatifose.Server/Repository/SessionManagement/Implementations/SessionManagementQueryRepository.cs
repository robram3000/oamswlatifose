using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Repository.SessionManagement.Interfaces;

namespace oamswlatifose.Server.Repository.SessionManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for user session data retrieval operations.
    /// This repository provides comprehensive read-only access to session information,
    /// user activity tracking, and concurrent session monitoring essential for
    /// security auditing and session management.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Active session monitoring per user</para>
    /// <para>- Session validation by token</para>
    /// <para>- User session history and activity tracking</para>
    /// <para>- Inactivity and expiration monitoring</para>
    /// <para>- Device and location-based session analysis</para>
    /// <para>- Concurrent session limit enforcement support</para>
    /// </summary>
    public class SessionManagementQueryRepository : ISessionManagementQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SessionManagementQueryRepository> _logger;

        public SessionManagementQueryRepository(
            ApplicationDbContext context,
            ILogger<SessionManagementQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMSession>> GetAllSessionsAsync()
        {
            _logger.LogDebug("Retrieving all sessions");
            return await _context.EMSessions
                .Include(s => s.User)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        public async Task<EMSession> GetSessionByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving session with ID: {Id}", id);
            return await _context.EMSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<EMSession> GetSessionByTokenAsync(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new ArgumentException("Session token cannot be null or empty", nameof(sessionToken));

            _logger.LogDebug("Retrieving session by token");
            return await _context.EMSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        }

        public async Task<IEnumerable<EMSession>> GetActiveSessionsByUserIdAsync(int userId)
        {
            _logger.LogDebug("Retrieving active sessions for user ID: {UserId}", userId);
            var now = DateTime.UtcNow;

            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId &&
                           s.IsActive &&
                           s.ExpiresAt > now)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMSession>> GetSessionHistoryByUserIdAsync(int userId)
        {
            _logger.LogDebug("Retrieving session history for user ID: {UserId}", userId);
            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMSession>> GetExpiredSessionsAsync()
        {
            _logger.LogDebug("Retrieving expired sessions");
            var now = DateTime.UtcNow;

            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.ExpiresAt < now ||
                           (s.IsActive && s.ExpiresAt < now))
                .OrderByDescending(s => s.ExpiresAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMSession>> GetInactiveSessionsAsync(TimeSpan inactivityThreshold)
        {
            _logger.LogDebug("Retrieving inactive sessions with threshold: {Threshold}", inactivityThreshold);
            var cutoff = DateTime.UtcNow.Subtract(inactivityThreshold);

            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.IsActive &&
                           s.LastActivity.HasValue &&
                           s.LastActivity.Value < cutoff)
                .OrderBy(s => s.LastActivity)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMSession>> GetSessionsByDeviceTypeAsync(string deviceType)
        {
            _logger.LogDebug("Retrieving sessions for device type: {DeviceType}", deviceType);
            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.DeviceType == deviceType)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMSession>> GetSessionsByLocationAsync(string location)
        {
            _logger.LogDebug("Retrieving sessions for location: {Location}", location);
            return await _context.EMSessions
                .Include(s => s.User)
                .Where(s => s.Location == location)
                .OrderByDescending(s => s.LoginTime)
                .ToListAsync();
        }

        public async Task<bool> IsSessionValidAsync(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                return false;

            var now = DateTime.UtcNow;
            return await _context.EMSessions
                .AnyAsync(s => s.SessionToken == sessionToken &&
                              s.IsActive &&
                              s.ExpiresAt > now);
        }

        public async Task<Dictionary<int, int>> GetActiveSessionCountPerUserAsync()
        {
            _logger.LogDebug("Calculating active session count per user");
            var now = DateTime.UtcNow;

            return await _context.EMSessions
                .Where(s => s.IsActive && s.ExpiresAt > now)
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count);
        }

        public async Task<int> GetActiveSessionCountAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.EMSessions
                .CountAsync(s => s.IsActive && s.ExpiresAt > now);
        }
    }
}
