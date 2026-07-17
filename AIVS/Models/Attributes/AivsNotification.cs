namespace AIVS.Models.Attributes
{
    public class AivsNotification
    {
        public long Id { get; set; }

        public int? TargetUserId { get; set; }

        public string? TargetUsername { get; set; }

        public string? TargetRole { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string NotificationType { get; set; } = string.Empty;

        public long? Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public long? InspectionRequestId { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadDateTime { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDateTime { get; set; } = DateTime.Now;
    }
}
