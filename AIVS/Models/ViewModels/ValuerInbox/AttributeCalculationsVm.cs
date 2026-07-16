namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttributeCalculationsVm
    {
        public decimal? CalcUpdateTla { get; set; }

        public decimal? Tla { get; set; }

        public decimal? CalcUpdateWgba { get; set; }

        public decimal? AdjustedWgba { get; set; }

        public decimal? TotalValueNonRes { get; set; }

        public decimal? TotalValueUnutilisedLand { get; set; }

        public decimal? DRCFinalValue { get; set; }

        public string? CalculationStatus { get; set; }
    }
}
