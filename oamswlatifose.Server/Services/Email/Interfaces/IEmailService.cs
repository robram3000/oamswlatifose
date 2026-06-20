using oamswlatifose.Server.DTO.Email;

namespace oamswlatifose.Server.Services.Email.Interfaces
{
    /// <summary>
    /// Service interface for email operations providing comprehensive email sending capabilities,
    /// template management, and delivery tracking for the entire system.
    /// 
    /// <para>Core Capabilities:</para>
    /// <para>- Send templated emails with dynamic content replacement</para>
    /// <para>- OTP/verification code delivery for authentication</para>
    /// <para>- Password reset emails with secure tokens</para>
    /// <para>- Welcome emails for new user registration</para>
    /// <para>- Notification emails for system events</para>
    /// <para>- Report delivery via email attachments</para>
    /// <para>- HTML and plain text email support</para>
    /// <para>- Bulk email sending with rate limiting</para>
    /// <para>- Email tracking and delivery status monitoring</para>
    /// <para>- Template management (create, update, preview)</para>
    /// 
    /// <para>Security Features:</para>
    /// <para>- Rate limiting per recipient to prevent abuse</para>
    /// <para>- Email address validation and sanitization</para>
    /// <para>- Secure token embedding in email links</para>
    /// <para>- SPF/DKIM/DMARC compliance</para>
    /// <para>- Bounce handling and invalid email detection</para>
    /// </summary>
    public interface IEmailService
    {
        #region Basic Email Sending

        /// <summary>
        /// Sends a simple text email to a single recipient.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Plain text email body</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendTextEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Sends an HTML email to a single recipient.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlBody">HTML email body</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendHtmlEmailAsync(string to, string subject, string htmlBody);

        /// <summary>
        /// Sends an email with attachments to a single recipient.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (can be HTML or plain text)</param>
        /// <param name="attachments">List of file attachments</param>
        /// <param name="isHtml">Whether the body is HTML</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendEmailWithAttachmentsAsync(
            string to,
            string subject,
            string body,
            List<EmailAttachmentDTO> attachments,
            bool isHtml = true);

        /// <summary>
        /// Sends an email to multiple recipients.
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (can be HTML or plain text)</param>
        /// <param name="isHtml">Whether the body is HTML</param>
        /// <returns>Email send result with batch tracking ID</returns>
        Task<ServiceResponse<BulkEmailSendResultDTO>> SendBulkEmailAsync(
            List<string> recipients,
            string subject,
            string body,
            bool isHtml = true);

        #endregion

        #region Templated Emails

        /// <summary>
        /// Sends an email using a predefined template with dynamic data replacement.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="templateName">Name of the email template</param>
        /// <param name="templateData">Dictionary of placeholder values to replace in template</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendTemplatedEmailAsync(
            string to,
            string templateName,
            Dictionary<string, string> templateData);

        /// <summary>
        /// Sends a welcome email to newly registered users.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="loginUrl">URL for user to login</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendWelcomeEmailAsync(
            string to,
            string userName,
            string loginUrl);

        /// <summary>
        /// Sends an email verification OTP code to user.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="otpCode">One-time password code</param>
        /// <param name="expiryMinutes">OTP expiration time in minutes</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendEmailVerificationOtpAsync(
            string to,
            string userName,
            string otpCode,
            int expiryMinutes);

        /// <summary>
        /// Sends a password reset email with secure token link.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="resetLink">Password reset link with embedded token</param>
        /// <param name="expiryHours">Token expiration time in hours</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendPasswordResetEmailAsync(
            string to,
            string userName,
            string resetLink,
            int expiryHours);

        /// <summary>
        /// Sends a two-factor authentication (2FA) OTP code.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="otpCode">One-time password code</param>
        /// <param name="expiryMinutes">OTP expiration time in minutes</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendTwoFactorOtpAsync(
            string to,
            string userName,
            string otpCode,
            int expiryMinutes);

        /// <summary>
        /// Sends a notification about account lockout due to failed attempts.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="lockoutMinutes">Lockout duration in minutes</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendAccountLockoutNotificationAsync(
            string to,
            string userName,
            int lockoutMinutes);

        /// <summary>
        /// Sends a notification about successful password change.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">User's full name</param>
        /// <param name="changeTime">Time of password change</param>
        /// <param name="ipAddress">IP address from which change was made</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendPasswordChangedNotificationAsync(
            string to,
            string userName,
            DateTime changeTime,
            string ipAddress);

        /// <summary>
        /// Sends an attendance report to manager or employee.
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="userName">Recipient's name</param>
        /// <param name="reportData">Attendance report data as byte array (PDF/Excel)</param>
        /// <param name="fileName">Name of the attached file</param>
        /// <param name="period">Report period description</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendAttendanceReportAsync(
            string to,
            string userName,
            byte[] reportData,
            string fileName,
            string period);

        /// <summary>
        /// Sends a system alert or notification to administrators.
        /// </summary>
        /// <param name="to">Administrator email address</param>
        /// <param name="alertType">Type of alert (Error, Warning, Info)</param>
        /// <param name="message">Alert message</param>
        /// <param name="details">Detailed information about the alert</param>
        /// <returns>Email send result with tracking ID</returns>
        Task<ServiceResponse<EmailSendResultDTO>> SendSystemAlertAsync(
            string to,
            string alertType,
            string message,
            string details);

        #endregion

        #region Template Management

        /// <summary>
        /// Creates a new email template.
        /// </summary>
        /// <param name="template">Email template data</param>
        /// <returns>Created template with ID</returns>
        Task<ServiceResponse<EmailTemplateDTO>> CreateEmailTemplateAsync(CreateEmailTemplateDTO template);

        /// <summary>
        /// Updates an existing email template.
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <param name="template">Updated template data</param>
        /// <returns>Updated template</returns>
        Task<ServiceResponse<EmailTemplateDTO>> UpdateEmailTemplateAsync(int id, UpdateEmailTemplateDTO template);

        /// <summary>
        /// Gets an email template by name.
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <returns>Email template</returns>
        Task<ServiceResponse<EmailTemplateDTO>> GetEmailTemplateAsync(string templateName);

        /// <summary>
        /// Gets all email templates.
        /// </summary>
        /// <returns>List of all email templates</returns>
        Task<ServiceResponse<IEnumerable<EmailTemplateDTO>>> GetAllEmailTemplatesAsync();

        /// <summary>
        /// Deletes an email template.
        /// </summary>
        /// <param name="id">Template ID</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> DeleteEmailTemplateAsync(int id);

        /// <summary>
        /// Previews an email template with sample data.
        /// </summary>
        /// <param name="templateName">Template name</param>
        /// <param name="sampleData">Sample data for placeholders</param>
        /// <returns>Rendered email preview</returns>
        Task<ServiceResponse<EmailPreviewDTO>> PreviewEmailTemplateAsync(
            string templateName,
            Dictionary<string, string> sampleData);

        #endregion

        #region Email Tracking and Analytics

        /// <summary>
        /// Tracks email open status when recipient opens the email.
        /// </summary>
        /// <param name="trackingId">Unique email tracking ID</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> TrackEmailOpenAsync(string trackingId);

        /// <summary>
        /// Tracks email click on links within the email.
        /// </summary>
        /// <param name="trackingId">Unique email tracking ID</param>
        /// <param name="linkUrl">URL that was clicked</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> TrackEmailClickAsync(string trackingId, string linkUrl);

        /// <summary>
        /// Records email bounce (failed delivery).
        /// </summary>
        /// <param name="trackingId">Unique email tracking ID</param>
        /// <param name="bounceReason">Reason for bounce</param>
        /// <returns>Success indicator</returns>
        Task<ServiceResponse<bool>> RecordEmailBounceAsync(string trackingId, string bounceReason);

        /// <summary>
        /// Gets email delivery statistics for a date range.
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Email statistics</returns>
        Task<ServiceResponse<EmailStatisticsDTO>> GetEmailStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets delivery status for a specific email.
        /// </summary>
        /// <param name="trackingId">Email tracking ID</param>
        /// <returns>Email delivery status</returns>
        Task<ServiceResponse<EmailDeliveryStatusDTO>> GetEmailDeliveryStatusAsync(string trackingId);

        #endregion

        #region Configuration and Testing

        /// <summary>
        /// Tests the SMTP connection with current settings.
        /// </summary>
        /// <returns>Connection test result</returns>
        Task<ServiceResponse<bool>> TestSmtpConnectionAsync();

        /// <summary>
        /// Sends a test email to verify configuration.
        /// </summary>
        /// <param name="to">Test recipient email address</param>
        /// <returns>Test result</returns>
        Task<ServiceResponse<bool>> SendTestEmailAsync(string to);

        /// <summary>
        /// Gets the current email service configuration.
        /// </summary>
        /// <returns>Email configuration settings</returns>
        Task<ServiceResponse<EmailConfigurationDTO>> GetEmailConfigurationAsync();

        /// <summary>
        /// Updates email service configuration.
        /// </summary>
        /// <param name="config">New configuration</param>
        /// <returns>Updated configuration</returns>
        Task<ServiceResponse<EmailConfigurationDTO>> UpdateEmailConfigurationAsync(UpdateEmailConfigurationDTO config);

        #endregion
    }
}
