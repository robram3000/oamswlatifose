namespace oamswlatifose.Server.Smtp
{
    public class DefaultSenderEmail
    {
        public string EmailAddress { get; set; } = "noreply@yourapp.com";
        public string DisplayName { get; set; } = "Your Application";

        public DefaultSenderEmail() { }

        public DefaultSenderEmail(string emailAddress, string displayName)
        {
            EmailAddress = emailAddress;
            DisplayName = displayName;
        }

        public (string Email, string Name) GetSenderInfo()
        {
            return (EmailAddress, DisplayName);
        }
    }
}
