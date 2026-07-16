namespace AIVS.Models.Attributes
{
    public class AttrValuerReview
    {
        public long Id { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public long? AssignmentId { get; set; }

        public int ReviewerUserId { get; set; }

        public string? ReviewerUsername { get; set; }

        public string? ReviewerName { get; set; }

        public string? ReviewerEmail { get; set; }

        public string? ReviewerRole { get; set; }

        public string ReviewStatus { get; set; } = "InProgress";

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        public string? FinalDecision { get; set; }

        public string? FinalComment { get; set; }

        public bool RequiresInspection { get; set; }

        public bool ReturnToClient { get; set; }

        public bool ReadyForOvvioExtract { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
