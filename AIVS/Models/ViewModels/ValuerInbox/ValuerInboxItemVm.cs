namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerInboxItemVm
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Township { get; set; }

        public string? Sector { get; set; }

        public string? Status { get; set; }

        public DateTime? AssignedDate { get; set; }

        public string? AssignedBy { get; set; }

        public int EvidenceCount { get; set; }

        public bool ReviewStarted { get; set; }

        public long? ReviewId { get; set; }
    }
}
