namespace AIVS.Models.ViewModels.SectorInbox
{
    public class SectorAssignmentResultVm
    {
        public int AssignedCount { get; set; }

        public string? Sector { get; set; }

        public List<string> AssignedReferences { get; set; } = new();
    }
}
