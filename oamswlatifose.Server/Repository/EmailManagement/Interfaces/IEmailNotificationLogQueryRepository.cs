using oamswlatifose.Server.Model.smtp;

namespace oamswlatifose.Server.Repository.EmailManagement.Interfaces
{
    /// <summary>
    /// Interface for email notification log query operations providing comprehensive read-only access
    /// to email communication records, delivery status information, and OTP verification tracking.
    /// This repository interface defines contract methods for retrieving email log entries essential
    /// for communication auditing, delivery monitoring, and user verification workflows.
    /// 
    /// <para>Core Query Capabilities:</para>
    /// <para>- Email log retrieval with complete metadata (recipient, timestamp, content summary)</para>
    /// <para>- OTP verification request tracking and status monitoring</para>
    /// <para>- Email delivery status analysis and failure investigation</para>
    /// <para>- User-specific email communication history</para>
    /// <para>- Time-based email volume analysis for operational monitoring</para>
    /// <para>- OTP validation and expiration status checking</para>
    /// 
    /// <para>All query methods support email communication monitoring, delivery troubleshooting,
    /// and user verification workflows essential for system notifications and security processes.</para>
    /// </summary>
    public interface IEmailNotificationLogQueryRepository
    {
        /// <summary>
        /// Retrieves all email log entries from the system for comprehensive communication auditing.
        /// Provides complete history of all email notifications sent through the system.
        /// </summary>
        /// <returns>A task representing the asynchronous operation with complete collection of email log entities</returns>
        Task<IEnumerable<EMEmaillogs>> GetAllEmailLogsAsync();

        /// <summary>
        /// Retrieves a specific email log entry using its unique system-generated identifier.
        /// Provides complete email details including recipient, timestamp, and associated OTP information.
        /// </summary>
        /// <param name="id">The unique system identifier (primary key) of the email log entry</param>
        /// <returns>A task containing the email log entity if found; otherwise, null reference</returns>
        Task<EMEmaillogs> GetEmailLogByIdAsync(int id);

        /// <summary>
        /// Retrieves all email log entries associated with a specific recipient email address.
        /// Essential for user communication history and troubleshooting delivery issues.
        /// </summary>
        /// <param name="email">The recipient email address to filter logs by</param>
        /// <returns>A task containing collection of email log entries sent to the specified email address</returns>
        Task<IEnumerable<EMEmaillogs>> GetEmailLogsByRecipientAsync(string email);

        /// <summary>
        /// Retrieves a paginated list of email log entries for efficient display in communication dashboards.
        /// Implements server-side pagination to optimize performance when reviewing extensive email histories.
        /// </summary>
        /// <param name="pageNumber">The current page number (1-indexed, must be greater than 0)</param>
        /// <param name="pageSize">The number of log entries to display per page (1-100 range recommended)</param>
        /// <returns>A task with paginated email log results containing the specified page of entries</returns>
        Task<IEnumerable<EMEmaillogs>> GetEmailLogsPaginatedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Retrieves email log entries within a specified date range for communication volume analysis.
        /// Supports operational monitoring and email delivery performance reporting.
        /// </summary>
        /// <param name="startDate">The beginning date of the email log period (inclusive)</param>
        /// <param name="endDate">The ending date of the email log period (inclusive)</param>
        /// <returns>A task containing collection of email log entries within the specified date range</returns>
        Task<IEnumerable<EMEmaillogs>> GetEmailLogsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves OTP verification request details using the unique OTP identifier.
        /// Critical for validating one-time passwords during user verification workflows.
        /// </summary>
        /// <param name="otpId">The unique identifier of the OTP request</param>
        /// <returns>A task containing the OTP user request entity if found; otherwise, null reference</returns>
        Task<EMOtpUserRequest> GetOtpRequestByIdAsync(string otpId);

        /// <summary>
        /// Retrieves the most recent OTP request for a specific email address for verification flows.
        /// Enables retrieval of current verification codes during user validation processes.
        /// </summary>
        /// <param name="email">The email address associated with the OTP request</param>
        /// <returns>A task containing the most recent OTP user request entity for the specified email</returns>
        Task<EMOtpUserRequest> GetLatestOtpRequestByEmailAsync(string email);

        /// <summary>
        /// Verifies if a provided OTP code matches the most recent valid request for an email address.
        /// Implements expiration checking and attempts tracking for security enforcement.
        /// </summary>
        /// <param name="email">The email address to verify OTP for</param>
        /// <param name="otpCode">The OTP code to validate</param>
        /// <returns>A task containing boolean indicating whether the OTP is valid and not expired</returns>
        Task<bool> ValidateOtpCodeAsync(string email, string otpCode);

        /// <summary>
        /// Retrieves the total count of email notifications sent within a specified time period.
        /// Provides communication volume metrics for operational monitoring and capacity planning.
        /// </summary>
        /// <param name="startDate">The beginning date of the counting period (inclusive)</param>
        /// <param name="endDate">The ending date of the counting period (inclusive)</param>
        /// <returns>A task containing the total number of email notifications sent in the specified period</returns>
        Task<int> GetEmailSentCountByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves OTP usage statistics for security monitoring and abuse detection.
        /// Enables analysis of verification patterns and identification of suspicious activity.
        /// </summary>
        /// <param name="email">The email address to analyze OTP usage for</param>
        /// <param name="since">The start date for analyzing OTP request patterns</param>
        /// <returns>A task containing dictionary mapping OTP request dates to their associated codes</returns>
        Task<Dictionary<DateTime, string>> GetOtpUsageHistoryAsync(string email, DateTime since);
    }
}
