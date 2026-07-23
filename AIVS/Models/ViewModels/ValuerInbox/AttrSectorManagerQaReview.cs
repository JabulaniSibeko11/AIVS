namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttrSectorManagerQaReview
    {
        public long Id { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public long? ValuerReviewId { get; set; }

        public string? Sector { get; set; }

        public DateTime QaWeekStartDate { get; set; }

        public DateTime QaWeekEndDate { get; set; }

        public bool IsRandomlySelected { get; set; }

        public string? SelectionReason { get; set; }

        public int? ValuerUserId { get; set; }

        public string? ValuerName { get; set; }

        public DateTime? ValuerSubmittedAt { get; set; }

        public string QaStatus { get; set; } = "Pending";

        public int? SectorManagerUserId { get; set; }

        public string? SectorManagerUsername { get; set; }

        public string? SectorManagerName { get; set; }

        public string? SectorManagerEmail { get; set; }

        public string? QaDecision { get; set; }

        public string? QaComment { get; set; }

        public DateTime? QaStartedAt { get; set; }

        public DateTime? QaCompletedAt { get; set; }

        public string? ReviewedPdfPathBeforeQa { get; set; }

        public string? ReviewedPdfPathAfterQa { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
