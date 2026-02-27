using System.ComponentModel.DataAnnotations;

namespace oamswlatifose.Server.DTO.Email
{
    /// <summary>
    /// Email attachment data transfer object
    /// </summary>
    public class EmailAttachmentDTO
    {
        [Required]
        public string FileName { get; set; }

        [Required]
        public string Content { get; set; } // Base64 encoded

        public string ContentType { get; set; }
    }

    /// <summary>
    /// Email send result data transfer object
    /// </summary>
    public class EmailSendResultDTO
    {
        public string TrackingId { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Bulk email send result data transfer object
    /// </summary>
    public class BulkEmailSendResultDTO
    {
        public string BatchId { get; set; }
        public int TotalRecipients { get; set; }
        public List<string> Successful { get; set; }
        public List<(string Email, string Error)> Failed { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Create email template data transfer object
    /// </summary>
    public class CreateEmailTemplateDTO
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string HtmlBody { get; set; }

        public string TextBody { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Update email template data transfer object
    /// </summary>
    public class UpdateEmailTemplateDTO
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Email template data transfer object
    /// </summary>
    public class EmailTemplateDTO
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Email preview data transfer object
    /// </summary>
    public class EmailPreviewDTO
    {
        public string TemplateName { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string TextBody { get; set; }
        public List<string> Placeholders { get; set; }
    }

    /// <summary>
    /// Email configuration data transfer object
    /// </summary>
    public class EmailConfigurationDTO
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool EnableSsl { get; set; }
        public bool EnableTracking { get; set; }
        public int MaxEmailsPerMinute { get; set; }
        public int MaxEmailsPerHour { get; set; }
        public int MaxEmailsPerDay { get; set; }
        public int RateLimitPerRecipient { get; set; }
        public List<string> Templates { get; set; }
    }

    /// <summary>
    /// Update email configuration data transfer object
    /// </summary>
    public class UpdateEmailConfigurationDTO
    {
        public string SmtpServer { get; set; }
        public int? SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
        public bool? EnableSsl { get; set; }
        public bool? EnableTracking { get; set; }
    }

    /// <summary>
    /// Email statistics data transfer object
    /// </summary>
    public class EmailStatisticsDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalSent { get; set; }
        public int Delivered { get; set; }
        public int Opened { get; set; }
        public int Clicked { get; set; }
        public int Bounced { get; set; }
        public int Complained { get; set; }
        public double OpenRate { get; set; }
        public double ClickRate { get; set; }
        public double BounceRate { get; set; }
        public List<DailyEmailStatsDTO> DailyBreakdown { get; set; }
    }

    /// <summary>
    /// Daily email statistics data transfer object
    /// </summary>
    public class DailyEmailStatsDTO
    {
        public DateTime Date { get; set; }
        public int Sent { get; set; }
        public int Opened { get; set; }
        public int Clicked { get; set; }
    }

    /// <summary>
    /// Email delivery status data transfer object
    /// </summary>
    public class EmailDeliveryStatusDTO
    {
        public string TrackingId { get; set; }
        public string Status { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? OpenedAt { get; set; }
        public int OpenedCount { get; set; }
        public List<EmailClickDTO> Clicks { get; set; }
        public string BounceReason { get; set; }
    }

    /// <summary>
    /// Email click tracking data transfer object
    /// </summary>
    public class EmailClickDTO
    {
        public string LinkUrl { get; set; }
        public DateTime ClickedAt { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
