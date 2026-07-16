namespace AIVS.Models.Attributes
{
    public class AttrValuerAssignment
    {
        public long Id { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public int AssignedToUserId { get; set; }

        public string? AssignedToUsername { get; set; }

        public string? AssignedToName { get; set; }

        public string? AssignedToEmail { get; set; }

        public string? AssignedToRole { get; set; }

        public string? AssignedSector { get; set; }

        public string AssignmentType { get; set; } = "SelfAssigned";

        public string AssignmentStatus { get; set; } = "Active";

        public int? AssignedByUserId { get; set; }

        public string? AssignedByUsername { get; set; }

        public string? AssignedByName { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public DateTime? ReleasedAt { get; set; }

        public int? ReleasedByUserId { get; set; }

        public string? ReleaseReason { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
