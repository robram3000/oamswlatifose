using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.TokenManagement.Interfaces
{ /// <summary>
  /// Interface for JWT token data modification operations defining contracts for all create,
  /// update, delete, and revocation operations on authentication tokens. This repository interface
  /// establishes the pattern for secure token lifecycle management with comprehensive security controls.
  /// 
  /// <para>Command Operations Overview:</para>
  /// <para>- Secure token issuance with configurable expiration policies</para>
  /// <para>- Individual token revocation for logout and security incidents</para>
  /// <para>- Bulk token revocation for user sessions and security responses</para>
  /// <para>- Refresh token rotation during token renewal workflows</para>
  /// <para>- Automated cleanup of expired tokens for database maintenance</para>
  /// <para>- IP-based token revocation for security incident response</para>
  /// 
  /// <para>All methods enforce token expiration policies, maintain comprehensive
  /// revocation audit trails, and support security monitoring through metadata tracking.</para>
  /// </summary>
    public interface IJwtTokenManagementCommandRepository
    {
        /// <summary>
        /// Creates a new JWT token record for authenticated user sessions.
        /// Stores both access token and refresh token with appropriate expiration settings.
        /// </summary>
        /// <param name="token">The JWT token entity containing user ID, token strings, and expiration dates</param>
        /// <returns>A task representing the asynchronous operation with the newly created token entity</returns>
        Task<EMJWT> CreateTokenAsync(EMJWT token);

        /// <summary>
        /// Revokes a specific token, immediately invalidating it for authentication purposes.
        /// Records the revocation reason and timestamp for security auditing.
        /// </summary>
        /// <param name="tokenId">The unique identifier of the token to revoke</param>
        /// <param name="revokedReason">The reason for revocation</param>
        /// <returns>A task representing the asynchronous operation with boolean success indicator</returns>
        Task<bool> RevokeTokenAsync(int tokenId, string revokedReason);

        /// <summary>
        /// Revokes all active tokens for a specific user, effectively terminating all their active sessions.
        /// Used during password changes, security incidents, or when user account is deactivated.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose tokens should be revoked</param>
        /// <param name="revokedReason">The reason for bulk revocation</param>
        /// <returns>A task representing the asynchronous operation with count of revoked tokens</returns>
        Task<int> RevokeAllUserTokensAsync(int userId, string revokedReason);

        /// <summary>
        /// Updates a token's refresh token string during token refresh operations.
        /// Maintains the same access token while generating a new refresh token for the session.
        /// </summary>
        /// <param name="tokenId">The unique identifier of the token to update</param>
        /// <param name="newRefreshToken">The new refresh token string</param>
        /// <param name="newRefreshTokenExpiration">The expiration date and time for the new refresh token</param>
        /// <returns>A task representing the asynchronous operation with the updated token entity</returns>
        Task<EMJWT> RefreshTokenAsync(int tokenId, string newRefreshToken, DateTime newRefreshTokenExpiration);

        /// <summary>
        /// Permanently removes expired tokens from the database for maintenance and performance optimization.
        /// Implements configurable retention policy to balance audit requirements with database size management.
        /// </summary>
        /// <param name="expirationThreshold">Tokens expired before this date will be deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted tokens</returns>
        Task<int> CleanupExpiredTokensAsync(DateTime expirationThreshold);

        /// <summary>
        /// Revokes all tokens associated with a specific IP address for security incident response.
        /// Enables rapid response to suspicious activity originating from particular network locations.
        /// </summary>
        /// <param name="ipAddress">The IP address whose associated tokens should be revoked</param>
        /// <param name="revokedReason">The reason for revocation</param>
        /// <returns>A task representing the asynchronous operation with count of revoked tokens</returns>
        Task<int> RevokeTokensByIPAddressAsync(string ipAddress, string revokedReason);
    }
}
