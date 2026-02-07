using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.TokenManagement.Interfaces
{
    /// <summary>
    /// Interface for JWT token query operations providing comprehensive read-only access to authentication tokens,
    /// refresh tokens, and token lifecycle information. This repository interface defines contract methods
    /// for retrieving token data essential for authentication validation, token refresh workflows,
    /// and security monitoring of active and historical token usage.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Token validation and lookup by access token or refresh token strings</para>
    /// <para>- Active token retrieval for specific users and sessions</para>
    /// <para>- Expiration monitoring and token status verification</para>
    /// <para>- Revocation status checking for security enforcement</para>
    /// <para>- Token history analysis and user session tracking</para>
    /// <para>- IP address and user agent token association analysis</para>
    /// 
    /// <para>All query methods support the authentication and authorization infrastructure
    /// by providing efficient token verification and status checking operations essential
    /// for secure API access control and user session management.</para>
    /// </summary>
    public interface IJwtTokenManagementQueryRepository
    {
        /// <summary>
        /// Retrieves all JWT tokens stored in the system including both active and revoked tokens.
        /// Provides comprehensive token inventory for security auditing and administrative oversight.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of JWT token entities</returns>
        Task<IEnumerable<EMJWT>> GetAllTokensAsync();

        /// <summary>
        /// Retrieves a specific JWT token record using its unique system-generated identifier.
        /// Provides complete token details including expiration, revocation status, and metadata.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the token record</param>
        /// <returns>A task containing the token entity if found; otherwise, null reference</returns>
        Task<EMJWT> GetTokenByIdAsync(int id);

        /// <summary>
        /// Retrieves a JWT token record using the actual access token string for authentication validation.
        /// Essential for token verification during API request authorization and security enforcement.
        /// </summary>
        /// <param name="token">The JWT access token string to look up</param>
        /// <returns>A task containing the token entity if found with matching token; otherwise, null</returns>
        Task<EMJWT> GetTokenByAccessTokenAsync(string token);

        /// <summary>
        /// Retrieves a JWT token record using the refresh token string for token refresh workflows.
        /// Critical for refresh token validation during access token renewal operations.
        /// </summary>
        /// <param name="refreshToken">The refresh token string to look up</param>
        /// <returns>A task containing the token entity if found with matching refresh token; otherwise, null</returns>
        Task<EMJWT> GetTokenByRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Retrieves all active, non-expired, non-revoked tokens for a specific user.
        /// Used for managing active user sessions and preventing excessive token issuance.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose active tokens to retrieve</param>
        /// <returns>A task containing collection of active, valid token entities for the specified user</returns>
        Task<IEnumerable<EMJWT>> GetActiveTokensByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves all token records associated with a specific user including expired and revoked tokens.
        /// Provides complete token history for security analysis and user activity monitoring.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose token history to retrieve</param>
        /// <returns>A task containing collection of all token entities associated with the specified user</returns>
        Task<IEnumerable<EMJWT>> GetTokenHistoryByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves all tokens that have expired but have not yet been revoked or cleaned up.
        /// Supports background cleanup jobs and maintenance operations for token table management.
        /// </summary>
        /// <returns>A task containing collection of expired token entities requiring cleanup</returns>
        Task<IEnumerable<EMJWT>> GetExpiredTokensAsync();

        /// <summary>
        /// Retrieves all revoked tokens for security auditing and compliance monitoring.
        /// Enables analysis of token revocation patterns and potential security incidents.
        /// </summary>
        /// <returns>A task containing collection of revoked token entities</returns>
        Task<IEnumerable<EMJWT>> GetRevokedTokensAsync();

        /// <summary>
        /// Verifies the validity and active status of an access token for authorization decisions.
        /// Checks expiration, revocation status, and ensures token exists in the system.
        /// </summary>
        /// <param name="token">The JWT access token string to validate</param>
        /// <returns>A task containing boolean indicating whether the token is valid and active</returns>
        Task<bool> IsTokenValidAsync(string token);

        /// <summary>
        /// Verifies the validity and active status of a refresh token for token refresh workflows.
        /// Ensures refresh token is not expired, not revoked, and properly associated with a user.
        /// </summary>
        /// <param name="refreshToken">The refresh token string to validate</param>
        /// <returns>A task containing boolean indicating whether the refresh token is valid and active</returns>
        Task<bool> IsRefreshTokenValidAsync(string refreshToken);

        /// <summary>
        /// Retrieves the total count of active tokens in the system for monitoring and capacity planning.
        /// Provides metrics on active user sessions and token distribution.
        /// </summary>
        /// <returns>A task containing the total number of active, non-expired tokens</returns>
        Task<int> GetActiveTokenCountAsync();

        /// <summary>
        /// Retrieves token usage statistics grouped by IP address for security analysis.
        /// Helps identify unusual patterns, potential brute force attacks, or compromised tokens.
        /// </summary>
        /// <returns>A task containing dictionary mapping IP addresses to their associated token counts</returns>
        Task<Dictionary<string, int>> GetTokenCountByIPAddressAsync();
    }
}
