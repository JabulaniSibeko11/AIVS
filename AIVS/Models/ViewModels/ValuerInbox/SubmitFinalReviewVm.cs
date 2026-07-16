namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class SubmitFinalReviewVm
    {
        public long AttrId { get; set; }

        public long ReviewId { get; set; }

        public string FinalDecision { get; set; } = string.Empty;

        public string? FinalComment { get; set; }
    }
}
