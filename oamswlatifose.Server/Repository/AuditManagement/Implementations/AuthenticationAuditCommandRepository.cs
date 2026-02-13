using oamswlatifose.Server.Model;
using oamswlatifose.Server.Model.security;

namespace oamswlatifose.Server.Repository.AuditManagement.Implementations
{
    /// <summary>
    /// Command repository implementation for authentication audit log data modification operations.
    /// This repository handles all create operations for authentication events with comprehensive
    /// security context capture, forensic data collection, and compliance-grade audit trail maintenance.
    /// 
    /// <para>Key Operational Features:</para>
    /// <para>- Comprehensive authentication event logging with complete security context</para>
    /// <para>- User identification for successful authentications with account linkage</para>
    /// <para>- Username capture for failed attempts without requiring valid account</para>
    /// <para>- IP address and geolocation tracking for forensic analysis</para>
    /// <para>- Device fingerprinting through user agent parsing</para>
    /// <para>- Detailed failure reason capture for troubleshooting</para>
    /// <para>- Immutable audit trail with timestamp preservation</para>
    /// 
    /// <para>All operations create permanent, immutable audit records suitable for
    /// compliance requirements (SOX, HIPAA, GDPR, etc.) and security investigations.
    /// Audit logs should never be modified or deleted in production environments.</para>
    /// </summary>
    public class AuthenticationAuditCommandRepository : IAuthenticationAuditCommandRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthenticationAuditCommandRepository> _logger;

        /// <summary>
        /// Initializes a new instance of the AuthenticationAuditCommandRepository with required dependencies.
        /// Establishes database context connection and logging infrastructure for audit log operations.
        /// </summary>
        /// <param name="context">The application database context providing access to authentication log tables</param>
        /// <param name="logger">The logging service for capturing audit log creation events and error information</param>
        public AuthenticationAuditCommandRepository(
            ApplicationDbContext context,
            ILogger<AuthenticationAuditCommandRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new authentication log entry capturing a security event with complete forensic context.
        /// Records essential security metadata including timestamp, user identification, IP address,
        /// device information, and event outcome for comprehensive audit trail maintenance.
        /// </summary>
        /// <param name="authLog">The authentication log entity containing complete event details and security context</param>
        /// <returns>A task representing the asynchronous operation with the newly created authentication log entity</returns>
        /// <exception cref="ArgumentNullException">Thrown when the authLog parameter is null</exception>
        public async Task<EMAuthLog> CreateAuthLogAsync(EMAuthLog authLog)
        {
            if (authLog == null)
                throw new ArgumentNullException(nameof(authLog));

            // Ensure timestamp is set
            if (authLog.Timestamp == default)
                authLog.Timestamp = DateTime.UtcNow;

            await _context.EMAuthLogs.AddAsync(authLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created authentication log entry: {authLog.Action}, Success: {authLog.WasSuccessful}");
            return authLog;
        }

        /// <summary>
        /// Logs a successful authentication event with complete user and device context.
        /// Captures positive security events including user identification and session metadata
        /// for activity monitoring and access pattern analysis.
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
        public async Task<EMAuthLog> LogSuccessfulAuthenticationAsync(
            int userId,
            string username,
            string action,
            string ipAddress,
            string userAgent,
            string deviceType = null,
            string location = null,
            string details = null)
        {
            var authLog = new EMAuthLog
            {
                UserId = userId,
                UsernameAttempted = username,
                Action = action,
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = deviceType ?? DetermineDeviceType(userAgent),
                Location = location,
                Details = details,
                WasSuccessful = true,
                FailureReason = null
            };

            return await CreateAuthLogAsync(authLog);
        }

        /// <summary>
        /// Logs a failed authentication event with detailed failure context for security analysis.
        /// Captures unsuccessful attempts including username attempted, failure reason, and complete
        /// request metadata for brute force detection and account compromise investigation.
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
        public async Task<EMAuthLog> LogFailedAuthenticationAsync(
            string usernameAttempted,
            string action,
            string failureReason,
            string ipAddress,
            string userAgent,
            string deviceType = null,
            string location = null,
            string details = null)
        {
            var authLog = new EMAuthLog
            {
                UserId = null,
                UsernameAttempted = usernameAttempted,
                Action = action,
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = deviceType ?? DetermineDeviceType(userAgent),
                Location = location,
                Details = details,
                WasSuccessful = false,
                FailureReason = failureReason
            };

            return await CreateAuthLogAsync(authLog);
        }

        /// <summary>
        /// Logs a password change event with security context for audit trail maintenance.
        /// Records password modification activities including success/failure status and reasons
        /// for compliance with security policies and user activity monitoring.
        /// </summary>
        /// <param name="userId">The identifier of the user changing their password</param>
        /// <param name="username">The username of the account being modified</param>
        /// <param name="wasSuccessful">Indicates whether the password change operation succeeded</param>
        /// <param name="failureReason">The reason for failure if the operation was unsuccessful</param>
        /// <param name="ipAddress">The IP address from which the request originated</param>
        /// <param name="userAgent">The user agent string from the client application</param>
        /// <param name="details">Additional contextual information about the password change</param>
        /// <returns>A task representing the asynchronous operation with the created authentication log entity</returns>
        public async Task<EMAuthLog> LogPasswordChangeAsync(
            int userId,
            string username,
            bool wasSuccessful,
            string failureReason,
            string ipAddress,
            string userAgent,
            string details = null)
        {
            var authLog = new EMAuthLog
            {
                UserId = userId,
                UsernameAttempted = username,
                Action = "PasswordChange",
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = DetermineDeviceType(userAgent),
                Details = details,
                WasSuccessful = wasSuccessful,
                FailureReason = failureReason
            };

            return await CreateAuthLogAsync(authLog);
        }

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
        public async Task<EMAuthLog> LogPasswordResetAsync(
            int userId,
            string username,
            bool wasSuccessful,
            string failureReason,
            string ipAddress,
            string userAgent,
            string details = null)
        {
            var authLog = new EMAuthLog
            {
                UserId = userId,
                UsernameAttempted = username,
                Action = "PasswordReset",
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = DetermineDeviceType(userAgent),
                Details = details,
                WasSuccessful = wasSuccessful,
                FailureReason = failureReason
            };

            return await CreateAuthLogAsync(authLog);
        }

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
        public async Task<EMAuthLog> LogAccountLockoutAsync(
            int userId,
            string username,
            TimeSpan lockoutDuration,
            string ipAddress,
            string userAgent,
            string details = null)
        {
            var authLog = new EMAuthLog
            {
                UserId = userId,
                UsernameAttempted = username,
                Action = "AccountLockout",
                Timestamp = DateTime.UtcNow,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = DetermineDeviceType(userAgent),
                Details = details ?? $"Account locked for {lockoutDuration.TotalMinutes} minutes due to excessive failed attempts",
                WasSuccessful = true,
                FailureReason = null
            };

            return await CreateAuthLogAsync(authLog);
        }

        /// <summary>
        /// Permanently removes authentication log entries older than the specified retention threshold.
        /// Implements data retention policies for compliance with privacy regulations (GDPR, CCPA, etc.)
        /// while maintaining required audit history durations.
        /// </summary>
        /// <param name="retentionThreshold">Authentication logs older than this date will be permanently deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted log entries</returns>
        public async Task<int> PurgeAuthLogsAsync(DateTime retentionThreshold)
        {
            var oldLogs = await _context.EMAuthLogs
                .Where(l => l.Timestamp < retentionThreshold)
                .ToListAsync();

            _context.EMAuthLogs.RemoveRange(oldLogs);
            var result = await _context.SaveChangesAsync();

            _logger.LogInformation($"Purged {result} authentication log entries older than {retentionThreshold}");
            return result;
        }

        /// <summary>
        /// Determines the device type from user agent string for security profiling.
        /// Parses common user agent patterns to classify devices as Mobile, Tablet, Desktop, or Unknown.
        /// </summary>
        /// <param name="userAgent">The user agent string from HTTP request</param>
        /// <returns>A string indicating the detected device type</returns>
        private string DetermineDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("mobile") || userAgent.Contains("android") && !userAgent.Contains("tablet"))
                return "Mobile";

            if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "Tablet";

            if (userAgent.Contains("windows") || userAgent.Contains("mac") || userAgent.Contains("linux") || userAgent.Contains("x11"))
                return "Desktop";

            return "Other";
        }
    }
}
