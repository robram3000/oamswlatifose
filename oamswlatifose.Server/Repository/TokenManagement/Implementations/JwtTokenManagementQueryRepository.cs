using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;
using oamswlatifose.Server.Repository.TokenManagement.Interfaces;

namespace oamswlatifose.Server.Repository.TokenManagement.Implementations
{
    /// <summary>
    /// Query repository implementation for JWT token data retrieval operations.
    /// This repository provides comprehensive read-only access to authentication tokens,
    /// refresh tokens, and token lifecycle information essential for authentication
    /// validation and security monitoring.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Token lookup by access token or refresh token strings</para>
    /// <para>- Active token retrieval for user session management</para>
    /// <para>- Token validation and expiration checking</para>
    /// <para>- Revocation status monitoring</para>
    /// <para>- Token history and audit trail access</para>
    /// <para>- IP-based token analysis for security monitoring</para>
    /// </summary>
    public class JwtTokenManagementQueryRepository : IJwtTokenManagementQueryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JwtTokenManagementQueryRepository> _logger;

        public JwtTokenManagementQueryRepository(
            ApplicationDbContext context,
            ILogger<JwtTokenManagementQueryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EMJWT>> GetAllTokensAsync()
        {
            _logger.LogDebug("Retrieving all JWT tokens");
            return await _context.EMJWT
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<EMJWT> GetTokenByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving token with ID: {Id}", id);
            return await _context.EMJWT
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<EMJWT> GetTokenByAccessTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));

            _logger.LogDebug("Retrieving token by access token");
            return await _context.EMJWT
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task<EMJWT> GetTokenByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));

            _logger.LogDebug("Retrieving token by refresh token");
            return await _context.EMJWT
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }

        public async Task<IEnumerable<EMJWT>> GetActiveTokensByUserIdAsync(int userId)
        {
            _logger.LogDebug("Retrieving active tokens for user ID: {UserId}", userId);
            var now = DateTime.UtcNow;

            return await _context.EMJWT
                .Include(t => t.User)
                .Where(t => t.UserId == userId &&
                           !t.IsRevoked &&
                           t.ExpiresAt > now)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMJWT>> GetTokenHistoryByUserIdAsync(int userId)
        {
            _logger.LogDebug("Retrieving token history for user ID: {UserId}", userId);
            return await _context.EMJWT
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMJWT>> GetExpiredTokensAsync()
        {
            _logger.LogDebug("Retrieving expired tokens");
            var now = DateTime.UtcNow;

            return await _context.EMJWT
                .Include(t => t.User)
                .Where(t => t.ExpiresAt < now)
                .OrderByDescending(t => t.ExpiresAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EMJWT>> GetRevokedTokensAsync()
        {
            _logger.LogDebug("Retrieving revoked tokens");
            return await _context.EMJWT
                .Include(t => t.User)
                .Where(t => t.IsRevoked)
                .OrderByDescending(t => t.RevokedAt)
                .ToListAsync();
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var now = DateTime.UtcNow;
            return await _context.EMJWT
                .AnyAsync(t => t.Token == token &&
                              !t.IsRevoked &&
                              t.ExpiresAt > now);
        }

        public async Task<bool> IsRefreshTokenValidAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var now = DateTime.UtcNow;
            return await _context.EMJWT
                .AnyAsync(t => t.RefreshToken == refreshToken &&
                              !t.IsRevoked &&
                              t.RefreshTokenExpiresAt > now);
        }

        public async Task<int> GetActiveTokenCountAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.EMJWT
                .CountAsync(t => !t.IsRevoked && t.ExpiresAt > now);
        }

        public async Task<Dictionary<string, int>> GetTokenCountByIPAddressAsync()
        {
            _logger.LogDebug("Calculating token count by IP address");

            return await _context.EMJWT
                .Where(t => !t.IsRevoked)
                .GroupBy(t => t.IPAddress ?? "Unknown")
                .Select(g => new { IPAddress = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.IPAddress, g => g.Count);
        }
    }
}
