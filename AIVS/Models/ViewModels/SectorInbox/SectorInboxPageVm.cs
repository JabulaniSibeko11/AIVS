namespace AIVS.Models.ViewModels.SectorInbox
{
    public class SectorInboxPageVm
    {
        public string? SelectedSector { get; set; }

        public bool CurrentUserCanAssignToValuer { get; set; }

        public List<SectorInboxTileVm> Tiles { get; set; } = new();

        public List<SectorInboxItemVm> Items { get; set; } = new();

        public List<SectorValuerVm> Valuers { get; set; } = new();
    }
}
