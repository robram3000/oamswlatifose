/*
 * File: EmailService.cs
 * Description: Email service class providing email sending, OTP sending, and configuration management functionality
 * Namespace: oamswlatifose.Server.Services.Email
 */

using oamswlatifose.Server.Smtp;
using System.Net;
using System.Net.Mail;
using AutoMapper;
using oamswlatifose.Server.Repository.Rsmtp;

namespace oamswlatifose.Server.Services.Email
{
    /*
     * Email Service Interface
     * Defines the contract for email-related operations
     */
    public interface IEmailService
    {
        /*
         * Sends an email using EmailMessage object
         * @param message: EmailMessage containing all email details (recipient, subject, body, attachments, etc.)
         * @return: EmailResponse indicating success/failure and additional metadata
         */
        Task<EmailResponse> SendEmailAsync(EmailMessage message);

        /*
         * Sends an email using SendEmailRequest object
         * @param request: SendEmailRequest containing simplified email sending parameters
         * @return: EmailResponse indicating success/failure and additional metadata
         */
        Task<EmailResponse> SendEmailAsync(SendEmailRequest request);

        /*
         * Sends an OTP (One-Time Password) using SendOTPRequest object
         * @param request: SendOTPRequest containing OTP sending parameters (email, username, OTP length, expiry time)
         * @return: SendOTPResponse with OTP details and sending status
         */
        Task<SendOTPResponse> SendOTPAsync(SendOTPRequest request);

        /*
         * Sends an OTP using basic email and username parameters
         * @param email: Recipient's email address
         * @param userName: Recipient's name for personalization
         * @return: SendOTPResponse with OTP details and sending status
         */
        Task<SendOTPResponse> SendOTPAsync(string email, string userName);

        /*
         * Retrieves current email configuration settings
         * @return: EmailConfigurationDTO containing all email configuration parameters
         */
        Task<EmailConfigurationDTO> GetConfigurationAsync();

        /*
         * Updates email configuration with provided settings
         * @param configDto: EmailConfigurationDTO containing new configuration values
         * @return: boolean indicating whether the update was successful
         */
        Task<bool> UpdateConfigurationAsync(EmailConfigurationDTO configDto);
    }

    /*
     * Email Service Implementation
     * Provides concrete implementation of email service operations
     */
    public class EmailService : IEmailService
    {
        /*
         * Private Fields - Dependency Injected Services and Configurations
         */
        private readonly SmtpConfiguration _config;           // SMTP server configuration (host, port, credentials)
        private readonly DefaultSenderEmail _senderEmail;     // Default sender email and name information
        private readonly TemplateOTPVerification _otpTemplate; // OTP email template generator
        private readonly IMapper _mapper;                     // AutoMapper for object-to-object mapping
        private readonly ILogger<EmailService> _logger;       // Logger for tracking operations and errors (optional)
        private readonly IEmailLogRepository _logRepository;  // Repository for logging email sending activities (optional)

        /*
         * Constructor with Dependency Injection
         * @param config: SMTP configuration object (required)
         * @param senderEmail: Default sender email configuration (required)
         * @param otpTemplate: OTP email template generator (required)
         * @param mapper: AutoMapper instance for object mapping (required)
         * @param logger: ILogger instance for logging (optional)
         * @param logRepository: Email log repository for persistence (optional)
         * @throws ArgumentNullException: If config or mapper parameters are null
         */
        public EmailService(
            SmtpConfiguration config,
            DefaultSenderEmail senderEmail,
            TemplateOTPVerification otpTemplate,
            IMapper mapper,
            ILogger<EmailService> logger = null,
            IEmailLogRepository logRepository = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _senderEmail = senderEmail ?? new DefaultSenderEmail();
            _otpTemplate = otpTemplate ?? new TemplateOTPVerification(_senderEmail);
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger;
            _logRepository = logRepository;
        }

        /*
         * SendEmailAsync with EmailMessage Parameter
         * Primary email sending method with full email details
         * @param message: EmailMessage containing complete email information:
         *   - ToEmail: Recipient's email address
         *   - ToName: Recipient's display name
         *   - Subject: Email subject line
         *   - Body content (HTML or plain text)
         *   - Attachments: File attachments with content
         *   - CC/BCC: Carbon copy and blind carbon copy recipients
         *   - IsHtml: Flag indicating HTML email format
         * @return: EmailResponse containing:
         *   - Success: Boolean indicating operation success
         *   - Message: Descriptive message about the operation
         *   - EmailId: Unique identifier for the sent email
         *   - SentTime: Timestamp when email was sent
         */
        public async Task<EmailResponse> SendEmailAsync(EmailMessage message)
        {
            var emailLog = _mapper.Map<EmailLog>(message);
            emailLog.MessageId = Guid.NewGuid().ToString();

            try
            {
                using (var smtpClient = CreateSmtpClient())
                using (var mailMessage = CreateMailMessage(message))
                {
                    emailLog.SentAt = DateTime.UtcNow;
                    await smtpClient.SendMailAsync(mailMessage);

                    emailLog.Status = "Sent";
                    emailLog.BodyPreview = GetBodyPreview(message);

                    await LogEmailAsync(emailLog);

                    _logger?.LogInformation("Email sent successfully to {ToEmail} with ID {MessageId}",
                        message.ToEmail, emailLog.MessageId);

                    return new EmailResponse
                    {
                        Success = true,
                        Message = "Email sent successfully",
                        EmailId = emailLog.MessageId,
                        SentTime = emailLog.SentAt.Value
                    };
                }
            }
            catch (Exception ex)
            {
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;
                emailLog.SentAt = DateTime.UtcNow;

                await LogEmailAsync(emailLog);

                _logger?.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);

                return new EmailResponse
                {
                    Success = false,
                    Message = $"Failed to send email: {ex.Message}",
                    SentTime = DateTime.UtcNow
                };
            }
        }

        /*
         * SendEmailAsync with SendEmailRequest Parameter
         * Simplified email sending method using request DTO
         * @param request: SendEmailRequest containing:
         *   - ToEmail: Recipient email address
         *   - ToName: Recipient name
         *   - Subject: Email subject
         *   - Body: Email body content
         *   - IsHtml: HTML format flag
         * @return: EmailResponse with sending status and metadata
         */
        public async Task<EmailResponse> SendEmailAsync(SendEmailRequest request)
        {
            var emailMessage = _mapper.Map<EmailMessage>(request);
            return await SendEmailAsync(emailMessage);
        }

        /*
         * SendOTPAsync with SendOTPRequest Parameter
         * Sends a One-Time Password (OTP) for user verification
         * @param request: SendOTPRequest containing:
         *   - Email: Recipient's email address
         *   - UserName: Recipient's name for personalization
         *   - OTPLength: Length of OTP code (optional, defaults to 6)
         *   - ExpiryMinutes: OTP validity period in minutes (optional, defaults to 10)
         * @return: SendOTPResponse containing:
         *   - Success: Boolean indicating operation success
         *   - Message: Descriptive message
         *   - OTP: Generated OTP code (only returned on success)
         *   - ExpiryTime: DateTime when OTP expires
         */
        public async Task<SendOTPResponse> SendOTPAsync(SendOTPRequest request)
        {
            try
            {
                // Generate OTP with specified length and expiry
                var (otp, expiry) = OTPGenerator.GenerateOTPWithExpiry(
                    request.OTPLength,      // Length of OTP code
                    request.ExpiryMinutes   // Expiry time in minutes
                );

                // Create email request using mapper
                var emailRequest = new SendEmailRequest
                {
                    ToEmail = request.Email,       // Recipient email
                    ToName = request.UserName,     // Recipient name
                    Subject = $"Your Verification Code: {otp}",  // Dynamic subject with OTP
                    Body = _otpTemplate.GenerateOTPEmailTemplate(otp, request.UserName, request.ExpiryMinutes),
                    IsHtml = true                 // OTP emails are typically HTML formatted
                };

                var emailResponse = await SendEmailAsync(emailRequest);

                if (emailResponse.Success)
                {
                    return new SendOTPResponse
                    {
                        Success = true,
                        Message = "OTP sent successfully",
                        OTP = otp,          // Return generated OTP for verification
                        ExpiryTime = expiry  // Return expiry time
                    };
                }
                else
                {
                    return new SendOTPResponse
                    {
                        Success = false,
                        Message = emailResponse.Message,
                        ExpiryTime = expiry  // Still return expiry even if sending failed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send OTP to {Email}", request.Email);

                return new SendOTPResponse
                {
                    Success = false,
                    Message = $"Failed to send OTP: {ex.Message}"
                };
            }
        }

        /*
         * SendOTPAsync with Email and UserName Parameters
         * Simplified OTP sending method with basic parameters
         * @param email: Recipient's email address (string)
         * @param userName: Recipient's name for personalization (string)
         * @return: SendOTPResponse with OTP details and sending status
         */
        public async Task<SendOTPResponse> SendOTPAsync(string email, string userName)
        {
            var request = new SendOTPRequest
            {
                Email = email,      // Set recipient email
                UserName = userName // Set recipient name
            };

            return await SendOTPAsync(request);
        }

        /*
         * GetConfigurationAsync
         * Retrieves current email system configuration
         * @return: EmailConfigurationDTO containing:
         *   - Host: SMTP server hostname
         *   - Port: SMTP server port number
         *   - IsEnabled: Configuration status flag
         *   - SenderEmail: Default sender email address
         *   - SenderName: Default sender display name
         */
        public async Task<EmailConfigurationDTO> GetConfigurationAsync()
        {
            var configDto = _mapper.Map<EmailConfigurationDTO>(_senderEmail);
            configDto.Host = _config.Host;
            configDto.Port = _config.Port;
            configDto.IsEnabled = !string.IsNullOrEmpty(_config.Host);

            return await Task.FromResult(configDto);
        }

        /*
         * UpdateConfigurationAsync
         * Updates email system configuration with provided values
         * @param configDto: EmailConfigurationDTO containing:
         *   - Host: New SMTP server hostname
         *   - Port: New SMTP server port
         *   - SenderEmail: New sender email address
         *   - SenderName: New sender display name
         *   - Other configuration properties
         * @return: boolean indicating whether update was successful
         * @note: Password updates should be handled separately for security reasons
         */
        public async Task<bool> UpdateConfigurationAsync(EmailConfigurationDTO configDto)
        {
            try
            {
                // Update sender configuration (email and name)
                _mapper.Map(configDto, _senderEmail);

                // Update SMTP server configuration (host, port, etc.)
                _mapper.Map(configDto, _config);

                // Security Note: Password should be updated separately
                // to prevent accidental exposure or insecure handling

                _logger?.LogInformation("Email configuration updated");
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to update email configuration");
                return false;
            }
        }

        /*
         * CreateSmtpClient - Private Helper Method
         * Creates and configures an SMTP client based on current configuration
         * @return: Configured SmtpClient instance ready for sending emails
         */
        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_config.Host, _config.Port)
            {
                EnableSsl = _config.EnableSsl,                    // SSL/TLS encryption
                UseDefaultCredentials = _config.UseDefaultCredentials, // Windows authentication
                Credentials = new NetworkCredential(_config.UserName, _config.Password), // Login credentials
                Timeout = _config.Timeout,                        // Connection timeout
                DeliveryMethod = SmtpDeliveryMethod.Network       // Network delivery method
            };
        }

        /*
         * CreateMailMessage - Private Helper Method
         * Creates a MailMessage object from EmailMessage DTO
         * @param message: EmailMessage containing email details
         * @return: Configured MailMessage instance ready for sending
         */
        private MailMessage CreateMailMessage(EmailMessage message)
        {
            // Get sender information from configuration
            var (senderEmail, senderName) = _senderEmail.GetSenderInfo();

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName), // Sender address with display name
                Subject = message.Subject,                        // Email subject
                Body = message.IsHtml ? message.HtmlBody : message.PlainTextBody, // Body content
                IsBodyHtml = message.IsHtml,                      // HTML format flag
                Priority = MailPriority.Normal                    // Email priority
            };

            // Add primary recipient
            mailMessage.To.Add(new MailAddress(message.ToEmail, message.ToName));

            // Add CC recipients (if any)
            foreach (var ccEmail in message.Cc)
            {
                if (!string.IsNullOrWhiteSpace(ccEmail))
                    mailMessage.CC.Add(ccEmail);
            }

            // Add BCC recipients (if any)
            foreach (var bccEmail in message.Bcc)
            {
                if (!string.IsNullOrWhiteSpace(bccEmail))
                    mailMessage.Bcc.Add(bccEmail);
            }

            // Add file attachments (if any)
            foreach (var attachment in message.Attachments)
            {
                var stream = new System.IO.MemoryStream(attachment.Content);
                mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
            }

            // Add plain text alternative view for HTML emails (for email clients that don't support HTML)
            if (message.IsHtml && !string.IsNullOrEmpty(message.PlainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody,
                    null,
                    "text/plain");
                mailMessage.AlternateViews.Add(plainTextView);
            }

            return mailMessage;
        }

        /*
         * LogEmailAsync - Private Helper Method
         * Logs email sending activity to the repository
         * @param emailLog: EmailLog entity containing email sending details
         */
        private async Task LogEmailAsync(EmailLog emailLog)
        {
            if (_logRepository != null)
            {
                await _logRepository.AddAsync(emailLog);
            }
        }

        /*
         * GetBodyPreview - Private Helper Method
         * Creates a short preview of the email body for logging purposes
         * @param message: EmailMessage containing the full body content
         * @return: String containing first 100 characters of body with ellipsis if truncated
         */
        private string GetBodyPreview(EmailMessage message)
        {
            var body = message.IsHtml ? message.HtmlBody : message.PlainTextBody;
            return body?.Length > 100 ? body.Substring(0, 100) + "..." : body ?? string.Empty;
        }
    }
}