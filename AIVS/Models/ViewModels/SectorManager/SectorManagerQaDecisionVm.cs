namespace AIVS.Models.ViewModels.SectorManager
{
    public class SectorManagerQaDecisionVm
    {
        public long QaId { get; set; }

        public long AttrId { get; set; }

        public string Decision { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;
    }
}