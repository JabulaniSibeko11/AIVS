namespace AIVS.Models.Attributes
{
    public class AttrValuerReviewSection
    {
        public long Id { get; set; }

        public long ReviewId { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public string SectionCode { get; set; } = string.Empty;

        public string SectionName { get; set; } = string.Empty;

        public string? SectionDecision { get; set; }

        public string? SectionComment { get; set; }

        public bool RequiresCorrection { get; set; }

        public bool RequiresInspection { get; set; }

        public int? ReviewedByUserId { get; set; }

        public string? ReviewedByName { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
