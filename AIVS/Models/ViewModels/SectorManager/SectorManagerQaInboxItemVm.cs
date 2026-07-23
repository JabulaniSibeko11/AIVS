namespace AIVS.Models.ViewModels.SectorManager
{
    public class SectorManagerQaInboxItemVm
    {
        public long QaId { get; set; }

        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public long? ValuerReviewId { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Township { get; set; }

        public string? Sector { get; set; }

        public string? ValuerName { get; set; }

        public DateTime? ValuerSubmittedAt { get; set; }

        public string? QaStatus { get; set; }

        public string? SelectionReason { get; set; }

        public DateTime QaWeekStartDate { get; set; }

        public DateTime QaWeekEndDate { get; set; }

        public string StatusDisplay => QaStatus?.Trim() switch
        {
            "Pending" => "Pending QA",
            "InProgress" => "QA In Progress",
            "Approved" => "Approved to OVVIO",
            "ReturnedToValuer" => "Returned to Valuer",
            _ => string.IsNullOrWhiteSpace(QaStatus) ? "Unknown" : QaStatus
        };
    }
}
