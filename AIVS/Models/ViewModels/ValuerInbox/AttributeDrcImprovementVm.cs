namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttributeDrcImprovementVm
    {
        public string? ImprovementDescription { get; set; }

        public string? Quality { get; set; }

        public decimal? AreaUnit { get; set; }

        public string? Condition { get; set; }

        public decimal? DepreciationPercentage { get; set; }

        public decimal? RatePerSQM { get; set; }

        public decimal? DepreciatedRate { get; set; }

        public decimal? ReplacementCost { get; set; }
    }
}
