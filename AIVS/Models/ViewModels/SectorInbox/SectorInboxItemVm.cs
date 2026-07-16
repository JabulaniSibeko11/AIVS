namespace AIVS.Models.ViewModels.SectorInbox
{
    public class SectorInboxItemVm
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Township { get; set; }

        public string? RoutedSector { get; set; }

        public DateTime SubmittedDate { get; set; }

        public DateTime? RoutedDate { get; set; }

        public int EvidenceCount { get; set; }

        public string? Status { get; set; }
    }
}
