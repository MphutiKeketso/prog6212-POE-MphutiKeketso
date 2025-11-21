namespace Contract_Monthly_Claim_System.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSSL { get; set; }
    }

    public class FileUploadSettings
    {
        public int MaxFileSizeMB { get; set; } = 10;
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public string UploadPath { get; set; } = "wwwroot/uploads/documents";
    }

    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationHours { get; set; } = 24;
    }
}
