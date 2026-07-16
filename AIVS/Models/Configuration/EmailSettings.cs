namespace AIVS.Models.Configuration
{
    public class EmailSettings
    {
        public bool Enabled { get; set; } = true;

        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 25;
        public bool UseSsl { get; set; }
        public bool UseDefaultCredentials { get; set; } = true;

        public string? Username { get; set; }
        public string? Password { get; set; }

        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;

        public List<string> DefaultCc { get; set; } = new();
        public List<string> DefaultBcc { get; set; } = new();

        public string? SystemSupportEmail { get; set; }

        public EmailTemplateSettings Templates { get; set; } = new();
    }

    public class EmailTemplateSettings
    {
        public string InspectionDateOptionsSubject { get; set; } = string.Empty;
        public string InspectionConfirmedSubject { get; set; } = string.Empty;
        public string ReturnedToClientSubject { get; set; } = string.Empty;
        public string AcceptedSubject { get; set; } = string.Empty;
        public string RejectedSubject { get; set; } = string.Empty;
    }
}
