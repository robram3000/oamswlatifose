namespace oamswlatifose.Server.Smtp
{
    public class SendEmailRequest
    {
        public string ToEmail { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; } = true;
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
    }

    public class SendOTPRequest
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public int OTPLength { get; set; } = 6;
        public int ExpiryMinutes { get; set; } = 10;
    }

    public class SendOTPResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string OTP { get; set; }
        public DateTime ExpiryTime { get; set; }
    }

    public class EmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string EmailId { get; set; }
        public DateTime SentTime { get; set; }
    }

    public class EmailConfigurationDTO
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
