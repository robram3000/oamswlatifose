using Microsoft.EntityFrameworkCore;
using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.TokenManagement.Implementations
{
    /// <summary>
    /// Command repository implementation for JWT token data modification operations.
    /// This repository handles all create, update, and delete operations for authentication tokens
    /// with comprehensive security validation, expiration management, and revocation capabilities.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Secure token issuance with configurable expiration policies</para>
    /// <para>- Refresh token generation with extended validity periods</para>
    /// <para>- Token revocation for security incidents and logout workflows</para>
    /// <para>- Automatic cleanup of expired tokens for database maintenance</para>
    /// <para>- Concurrent token session limiting per user</para>
    /// <para>- IP address and user agent tracking for security auditing</para>
    /// 
    /// <para>All operations maintain strict security standards including
    /// proper token expiration enforcement, revocation capabilities for compromised tokens,
    /// and comprehensive audit trails of all token lifecycle events.</para>
    /// </summary>
    public class JwtTokenManagementCommandRepository : IJwtTokenManagementCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JwtTokenManagementCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the JwtTokenManagementCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for token management operations.
        /// </summary>
        /// <param name="context">The application database context providing access to JWT token tables</param>
        /// <param name="logger">The logging service for capturing token management operation details and security events</param>
        public JwtTokenManagementCommandRepository(
            ApplicationDbContext context,
            ILogger<JwtTokenManagementCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new JWT token record for authenticated user sessions.
        /// Stores both access token and refresh token with appropriate expiration policies
        /// and tracks security metadata including IP address and user agent.
        /// </summary>
        /// <param name="token">The JWT token entity containing user ID, token strings, and expiration settings</param>
        /// <returns>A task representing the asynchronous operation with the newly created token entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the token parameter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the referenced user does not exist</exception>
        public async Task<EMJWT> CreateTokenAsync(EMJWT token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            // Verify user exists
            var userExists = await _context.EMAuthorizerusers.AnyAsync(u => u.Id == token.UserId);
            if (!userExists)
                throw new InvalidOperationException($"User with ID {token.UserId} not found");

            // Initialize token record
            token.CreatedAt = DateTime.UtcNow;
            token.IsRevoked = false;

            await _context.EMJWTs.AddAsync(token);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created JWT token for user {token.UserId}, expires at {token.ExpiresAt}");
            return token;
        }

        /// <summary>
        /// Revokes a specific token, immediately invalidating it for authentication purposes.
        /// Records the revocation reason and timestamp for security auditing and token lifecycle tracking.
        /// </summary>
        /// <param name="tokenId">The unique identifier of the token to revoke</param>
        /// <param name="revokedReason">The reason for revocation (e.g., "User logout", "Security compromise", "Admin action")</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no token exists with the specified Id</exception>
        public async Task<bool> RevokeTokenAsync(int tokenId, string revokedReason)
        {
            var token = await _context.EMJWTs
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            if (token == null)
                throw new KeyNotFoundException($"Token with ID {tokenId} not found");

            token.IsRevoked = true;
            token.RevokedReason = revokedReason;
            token.RevokedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Revoked token {tokenId} for user {token.UserId}: {revokedReason}");
            return result > 0;
        }

        /// <summary>
        /// Revokes all active tokens for a specific user, effectively terminating all their active sessions.
        /// Used during password changes, security incidents, or when user account is deactivated.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose tokens should be revoked</param>
        /// <param name="revokedReason">The reason for bulk revocation</param>
        /// <returns>A task representing the asynchronous operation with count of revoked tokens</returns>
        public async Task<int> RevokeAllUserTokensAsync(int userId, string revokedReason)
        {
            var tokens = await _context.EMJWTs
                .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            var utcNow = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedReason = revokedReason;
                token.RevokedAt = utcNow;
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Revoked {result} tokens for user {userId}: {revokedReason}");
            return result;
        }

        /// <summary>
        /// Updates a token's refresh token string during token refresh operations.
        /// Maintains the same access token while generating a new refresh token for the session.
        /// </summary>
        /// <param name="tokenId">The unique identifier of the token to update</param>
        /// <param name="newRefreshToken">The new refresh token string</param>
        /// <param name="newRefreshTokenExpiration">The expiration date and time for the new refresh token</param>
        /// <returns>A task representing the asynchronous operation with the updated token entity</returns>
        /// <exception cref="KeyNotFoundException">Thrown when no token exists with the specified Id</exception>
        public async Task<EMJWT> RefreshTokenAsync(int tokenId, string newRefreshToken, DateTime newRefreshTokenExpiration)
        {
            var token = await _context.EMJWTs
                .FirstOrDefaultAsync(t => t.Id == tokenId);
            if (token == null)
                throw new KeyNotFoundException($"Token with ID {tokenId} not found");

            if (token.IsRevoked)
                throw new InvalidOperationException("Cannot refresh a revoked token");

            if (token.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Cannot refresh an expired token");

            token.RefreshToken = newRefreshToken;
            token.RefreshTokenExpiresAt = newRefreshTokenExpiration;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Refreshed token {tokenId} for user {token.UserId}");
            return token;
        }

        /// <summary>
        /// Permanently removes expired tokens from the database for maintenance and performance optimization.
        /// Implements configurable retention policy to balance audit requirements with database size management.
        /// </summary>
        /// <param name="expirationThreshold">Tokens expired before this date will be deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted tokens</returns>
        public async Task<int> CleanupExpiredTokensAsync(DateTime expirationThreshold)
        {
            var expiredTokens = await _context.EMJWTs
                .Where(t => t.ExpiresAt < expirationThreshold)
                .ToListAsync();

            _context.EMJWTs.RemoveRange(expiredTokens);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Cleaned up {result} expired tokens older than {expirationThreshold}");
            return result;
        }

        /// <summary>
        /// Revokes all tokens associated with a specific IP address for security incident response.
        /// Enables rapid response to suspicious activity originating from particular network locations.
        /// </summary>
        /// <param name="ipAddress">The IP address whose associated tokens should be revoked</param>
        /// <param name="revokedReason">The reason for revocation</param>
        /// <returns>A task representing the asynchronous operation with count of revoked tokens</returns>
        public async Task<int> RevokeTokensByIPAddressAsync(string ipAddress, string revokedReason)
        {
            var tokens = await _context.EMJWTs
                .Where(t => t.IPAddress == ipAddress && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            var utcNow = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedReason = revokedReason;
                token.RevokedAt = utcNow;
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Revoked {result} tokens from IP address {ipAddress}: {revokedReason}");
            return result;
        }
    }
}
