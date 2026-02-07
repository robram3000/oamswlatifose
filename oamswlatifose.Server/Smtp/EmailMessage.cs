namespace oamswlatifose.Server.Smtp
{
    public class EmailMessage
    {
        public string ToEmail { get; set; }
        public string ToName { get; set; }
        public string Subject { get; set; }
        public string HtmlBody { get; set; }
        public string PlainTextBody { get; set; }
        public bool IsHtml { get; set; } = true;
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();

        public EmailMessage() { }

        public EmailMessage(string toEmail, string subject, string body)
        {
            ToEmail = toEmail;
            Subject = subject;
            HtmlBody = body;
            IsHtml = true;
        }
    }

    public class EmailAttachment
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
    }
}
