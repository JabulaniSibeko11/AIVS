namespace AIVS.Models.Attributes
{
    public class AivsEmailLog
    {
        public long Id { get; set; }

        public string EmailType { get; set; } = string.Empty;

        public string? Attr_No { get; set; }

        public long? Attr_ID { get; set; }

        public string? OriginalToEmail { get; set; }

        public string? ActualToEmail { get; set; }

        public string? CcEmails { get; set; }

        public string? BccEmails { get; set; }

        public string? Subject { get; set; }

        public string? BodyPreview { get; set; }

        public bool IsTestMode { get; set; }

        public string SendStatus { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? SentDate { get; set; }
    }
}
