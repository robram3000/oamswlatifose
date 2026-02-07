using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.AuditManagement.Interfaces
{
    /// <summary>
    /// Interface for authentication audit log data modification operations defining contracts for all create
    /// and maintenance operations on security audit records. This repository interface establishes the pattern
    /// for comprehensive security event logging with complete forensic context capture.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Comprehensive authentication event logging with complete security context</para>
    /// <para>- Successful authentication recording with user and device fingerprinting</para>
    /// <para>- Failed attempt logging with detailed failure reason analysis</para>
    /// <para>- Password change and reset event tracking for compliance</para>
    /// <para>- Account lockout event capture for security monitoring</para>
    /// <para>- Configurable audit log purging for data retention compliance</para>
    /// 
    /// <para>All methods create immutable audit records that serve as the definitive
    /// source of truth for security investigations and compliance audits.</para>
    /// </summary>
    public interface IAuthenticationAuditCommandRepository
    {
        /// <summary>
        /// Creates a new authentication log entry capturing a security event with complete forensic context.
        /// Records essential security metadata for comprehensive audit trail maintenance.
        /// </summary>
        /// <param name="authLog">The authentication log entity containing complete event details and security context</param>
        /// <returns>A task representing the asynchronous operation with the newly created authentication log entity</returns>
        Task<EMAuthLog> CreateAuthLogAsync(EMAuthLog authLog);

        /// <summary>
        /// Logs a successful authentication event with complete user and device context.
        /// Captures positive security events for activity monitoring and access pattern analysis.
        /// </summary>
        /// <param name="userId">The identifier of the authenticated user</param>
        /// <param name="username">The username that successfully authenticated</param>
        /// <param name="action">The authentication action performed (Login, TokenRefresh, etc.)</param>
        /// <param name="ipAddress">The IP address from which the authentication originated</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="deviceType">The detected device type (Mobile, Desktop, Tablet, etc.)</param>
        /// <param name="location">The geographic location derived from IP address</param>
        /// <param name="details">Additional contextual information about the authentication event</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        Task<EMAuthLog> LogSuccessfulAuthenticationAsync(
            int userId,
            string username,
            string action,
            string ipAddress,
            string userAgent,
            string deviceType = null,
            string location = null,
            string details = null);

        /// <summary>
        /// Logs a failed authentication event with detailed failure context for security analysis.
        /// Captures unsuccessful attempts for brute force detection and account compromise investigation.
        /// </summary>
        /// <param name="usernameAttempted">The username that was attempted during authentication</param>
        /// <param name="action">The authentication action that failed</param>
        /// <param name="failureReason">The specific reason for authentication failure</param>
        /// <param name="ipAddress">The IP address from which the failed attempt originated</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="deviceType">The detected device type (Mobile, Desktop, Tablet, etc.)</param>
        /// <param name="location">The geographic location derived from IP address</param>
        /// <param name="details">Additional contextual information about the failure</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        Task<EMAuthLog> LogFailedAuthenticationAsync(
            string usernameAttempted,
            string action,
            string failureReason,
            string ipAddress,
            string userAgent,
            string deviceType = null,
            string location = null,
            string details = null);

        /// <summary>
        /// Logs a password change event with security context for audit trail maintenance.
        /// Records password modification activities for compliance and security monitoring.
        /// </summary>
        /// <param name="userId">The identifier of the user changing their password</param>
        /// <param name="username">The username of the account being modified</param>
        /// <param name="wasSuccessful">Indicates whether the password change operation succeeded</param>
        /// <param name="failureReason">The reason for failure if the operation was unsuccessful</param>
        /// <param name="ipAddress">The IP address from which the request originated</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="details">Additional contextual information about the password change</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        Task<EMAuthLog> LogPasswordChangeAsync(
            int userId,
            string username,
            bool wasSuccessful,
            string failureReason,
            string ipAddress,
            string userAgent,
            string details = null);

        /// <summary>
        /// Logs a password reset event initiated through "forgot password" workflow.
        /// Captures password recovery operations with complete security context for audit compliance.
        /// </summary>
        /// <param name="userId">The identifier of the user resetting their password</param>
        /// <param name="username">The username of the account being reset</param>
        /// <param name="wasSuccessful">Indicates whether the password reset operation succeeded</param>
        /// <param name="failureReason">The reason for failure if the operation was unsuccessful</param>
        /// <param name="ipAddress">The IP address from which the request originated</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="details">Additional contextual information about the password reset</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        Task<EMAuthLog> LogPasswordResetAsync(
            int userId,
            string username,
            bool wasSuccessful,
            string failureReason,
            string ipAddress,
            string userAgent,
            string details = null);

        /// <summary>
        /// Logs account lockout events triggered by excessive failed authentication attempts.
        /// Captures security enforcement actions for monitoring and incident response.
        /// </summary>
        /// <param name="userId">The identifier of the locked out user account</param>
        /// <param name="username">The username of the locked out account</param>
        /// <param name="lockoutDuration">The duration of the account lockout</param>
        /// <param name="ipAddress">The IP address that triggered the lockout</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="details">Additional contextual information about the lockout</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        Task<EMAuthLog> LogAccountLockoutAsync(
            int userId,
            string username,
            TimeSpan lockoutDuration,
            string ipAddress,
            string userAgent,
            string details = null);

        /// <summary>
        /// Permanently removes authentication log entries older than the specified retention threshold.
        /// Implements data retention policies for compliance with privacy regulations.
        /// </summary>
        /// <param name="retentionThreshold">Authentication logs older than this date will be permanently deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted log entries</returns>
        Task<int> PurgeAuthLogsAsync(DateTime retentionThreshold);
    }
}
