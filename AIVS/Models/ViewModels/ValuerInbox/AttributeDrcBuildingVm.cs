namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttributeDrcBuildingVm
    {
        public string? BuildingDescription { get; set; }

        public string? Quality { get; set; }

        public decimal? GrossBuildingArea { get; set; }

        public string? Condition { get; set; }

        public decimal? DepreciationPercentage { get; set; }

        public decimal? RatePerSQM { get; set; }

        public decimal? DepreciatedRate { get; set; }

        public decimal? ReplacementCost { get; set; }
    }
}
