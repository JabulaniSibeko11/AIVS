namespace AIVS.Models.ViewModels.SectorInbox
{
    public class SectorManagerAssignmentRequestVm
    {
        public List<long> SelectedAttrIds { get; set; } = new();

        public int SelectedValuerUserId { get; set; }

        public string? Sector { get; set; }
    }
}
