namespace AIVS.Models.ViewModels.Notifications
{
    public class AivsNotificationVm
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string NotificationType { get; set; } = string.Empty;

        public long? AttrId { get; set; }

        public string? AttrNo { get; set; }

        public long? InspectionRequestId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedDateTime { get; set; }
    }
}
