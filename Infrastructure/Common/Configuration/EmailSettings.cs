namespace Infrastructure.Common.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@bookify.local";
        public string FromName { get; set; } = "Bookify Library";
        public bool EnableSsl { get; set; } = true;
    }
}
