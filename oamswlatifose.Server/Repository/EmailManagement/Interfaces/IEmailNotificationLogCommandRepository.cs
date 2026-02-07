using oamswlatifose.Server.Model.smtp;

namespace oamswlatifose.Server.Repository.EmailManagement.Interfaces
{
    /// <summary>
    /// Interface for email notification log and OTP verification data modification operations
    /// defining contracts for all create and maintenance operations on email communication records
    /// and one-time password verification requests. This repository interface establishes the pattern
    /// for secure email logging and OTP-based user verification workflows.
    /// 
    /// <para>Command Operations Overview:</para>
    /// <para>- Email notification logging with complete metadata capture</para>
    /// <para>- Secure OTP generation with cryptographic randomness</para>
    /// <para>- OTP verification with single-use enforcement</para>
    /// <para>- Email-OTP relationship management for verification workflows</para>
    /// <para>- Bulk OTP invalidation for security enforcement</para>
    /// <para>- Configurable audit log purging for data retention compliance</para>
    /// 
    /// <para>All methods implement secure OTP handling practices including
    /// cryptographic generation, expiration enforcement, and single-use policies.</para>
    /// </summary>
    public interface IEmailNotificationLogCommandRepository
    {
        /// <summary>
        /// Creates a new email log entry recording a sent notification with complete metadata.
        /// Captures essential communication details for audit trails and delivery verification.
        /// </summary>
        /// <param name="emailLog">The email log entity containing recipient, content, and metadata information</param>
        /// <returns>A task representing the asynchronous operation with the newly created email log entity</returns>
        Task<EMEmaillogs> CreateEmailLogAsync(EMEmaillogs emailLog);

        /// <summary>
        /// Generates a new one-time password (OTP) for user verification with secure random generation.
        /// Creates a cryptographically random numeric code with configurable length and expiration period.
        /// </summary>
        /// <param name="email">The email address requesting OTP verification</param>
        /// <param name="otpLength">The length of the OTP code to generate (default: 6 digits)</param>
        /// <param name="expiryMinutes">The number of minutes until the OTP expires (default: 10 minutes)</param>
        /// <returns>A task representing the asynchronous operation with the created OTP user request entity</returns>
        Task<EMOtpUserRequest> GenerateOtpAsync(string email, int otpLength = 6, int expiryMinutes = 10);

        /// <summary>
        /// Verifies a provided OTP code against the stored valid request for an email address.
        /// Implements security controls including expiration checking and single-use enforcement.
        /// </summary>
        /// <param name="email">The email address associated with the OTP request</param>
        /// <param name="otpCode">The OTP code to verify</param>
        /// <returns>A task representing the asynchronous operation with boolean verification result</returns>
        Task<bool> VerifyOtpAsync(string email, string otpCode);

        /// <summary>
        /// Creates a complete email notification record with associated OTP verification data.
        /// Combines email sending and OTP generation into a single transactional operation.
        /// </summary>
        /// <param name="email">The recipient email address</param>
        /// <param name="otpRequest">The OTP request entity associated with this email</param>
        /// <returns>A task representing the asynchronous operation with the created email log entity</returns>
        Task<EMEmaillogs> LogEmailWithOtpAsync(string email, EMOtpUserRequest otpRequest);

        /// <summary>
        /// Invalidates all existing OTP requests for a specific email address.
        /// Used during password changes, account lockout, or security events to prevent OTP reuse.
        /// </summary>
        /// <param name="email">The email address whose OTP requests should be invalidated</param>
        /// <returns>A task representing the asynchronous operation with count of invalidated OTPs</returns>
        Task<int> InvalidateExistingOtpsAsync(string email);

        /// <summary>
        /// Permanently removes email log entries older than the specified retention threshold.
        /// Implements data retention policies for compliance with privacy regulations.
        /// </summary>
        /// <param name="retentionThreshold">Email logs older than this date will be permanently deleted</param>
        /// <returns>A task representing the asynchronous operation with count of deleted log entries</returns>
        Task<int> PurgeEmailLogsAsync(DateTime retentionThreshold);
    }
}
