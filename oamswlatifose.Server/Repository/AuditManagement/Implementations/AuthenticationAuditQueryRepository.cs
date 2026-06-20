using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Repository.AuditManagement.Interfaces;

namespace oamswlatifose.Server.Repository.AuditManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for authentication audit log data retrieval operations.
    /// This repository provides comprehensive read-only access to security event information,
    /// user authentication history, and compliance reporting data essential for security
    /// monitoring and incident investigation.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Authentication event retrieval with multi-dimensional filtering</para>
    /// <para>- User-specific authentication history tracking</para>
    /// <para>- Failed attempt analysis for security monitoring</para>
    /// <para>- IP-based threat investigation</para>
    /// <para>- Compliance reporting with date-range queries</para>
    /// <para>- Device fingerprinting and pattern analysis</para>
    /// </summary>
    public class AuthenticationAuditQueryRepository : IAuthenticationAuditQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthenticationAuditQueryRepository> _logger;

        public AuthenticationAuditQueryRepository(
            ApplicationDbContext context,
            ILogger<AuthenticationAuditQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMAuthLog>> GetAllAuthLogsAsync()
        {
            _logger.LogDebug("Retrieving all authentication logs");
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<EMAuthLog> GetAuthLogByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving auth log with ID: {Id}", id);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsPaginatedAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            _logger.LogDebug("Retrieving auth logs page {PageNumber} with page size {PageSize}", pageNumber, pageSize);

            return await _context.EMAuthLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByUserIdAsync(int userId)
        {
            _logger.LogDebug("Retrieving auth logs for user ID: {UserId}", userId);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be null or empty", nameof(username));

            _logger.LogDebug("Retrieving auth logs for username: {Username}", username);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.UsernameAttempted == username)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetSuccessfulAuthLogsAsync()
        {
            _logger.LogDebug("Retrieving successful authentication logs");
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.WasSuccessful)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetFailedAuthLogsAsync()
        {
            _logger.LogDebug("Retrieving failed authentication logs");
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => !l.WasSuccessful)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogDebug("Retrieving auth logs from {StartDate} to {EndDate}",
                startDate.ToString("yyyy-MM-dd HH:mm"), endDate.ToString("yyyy-MM-dd HH:mm"));

            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByActionAsync(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be null or empty", nameof(action));

            _logger.LogDebug("Retrieving auth logs for action: {Action}", action);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.Action == action)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByIPAddressAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                throw new ArgumentException("IP address cannot be null or empty", nameof(ipAddress));

            _logger.LogDebug("Retrieving auth logs for IP address: {IPAddress}", ipAddress);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.IPAddress == ipAddress)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMAuthLog>> GetAuthLogsByDeviceTypeAsync(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                throw new ArgumentException("Device type cannot be null or empty", nameof(deviceType));

            _logger.LogDebug("Retrieving auth logs for device type: {DeviceType}", deviceType);
            return await _context.EMAuthLogs
                .Include(l => l.User)
                .Where(l => l.DeviceType == deviceType)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetFailedAttemptCountByIPAddressAsync(DateTime since)
        {
            _logger.LogDebug("Calculating failed attempts by IP address since {Since}", since.ToString("yyyy-MM-dd HH:mm"));

            return await _context.EMAuthLogs
                .Where(l => !l.WasSuccessful && l.Timestamp >= since)
                .GroupBy(l => l.IPAddress ?? "Unknown")
                .Select(g => new { IPAddress = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToDictionaryAsync(g => g.IPAddress, g => g.Count);
        }

        public async Task<Dictionary<string, int>> GetFailedAttemptCountByUsernameAsync(DateTime since)
        {
            _logger.LogDebug("Calculating failed attempts by username since {Since}", since.ToString("yyyy-MM-dd HH:mm"));

            return await _context.EMAuthLogs
                .Where(l => !l.WasSuccessful && l.Timestamp >= since)
                .GroupBy(l => l.UsernameAttempted ?? "Unknown")
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToDictionaryAsync(g => g.Username, g => g.Count);
        }

        public async Task<int> GetAuthEventCountByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.EMAuthLogs
                .CountAsync(l => l.Timestamp >= startDate && l.Timestamp <= endDate);
        }
    }
}
