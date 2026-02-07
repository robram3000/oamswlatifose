namespace oamswlatifose.Server.Smtp
{
    public class SmtpConfiguration
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public bool UseDefaultCredentials { get; set; } = false;
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Timeout { get; set; } = 10000; // 10 seconds

        public SmtpConfiguration() { }

        public SmtpConfiguration(string host, int port, string userName, string password)
        {
            Host = host;
            Port = port;
            UserName = userName;
            Password = password;
        }
    }
}
