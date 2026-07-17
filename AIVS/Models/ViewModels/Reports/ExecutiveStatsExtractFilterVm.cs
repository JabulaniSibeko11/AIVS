namespace AIVS.Models.ViewModels.Reports
{
    public class ExecutiveStatsExtractFilterVm
    {
        public string PeriodType { get; set; } = "Monthly";
        // Weekly, Monthly, Custom

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
