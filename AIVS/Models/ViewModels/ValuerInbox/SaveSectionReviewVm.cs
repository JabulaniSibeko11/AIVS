namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class SaveSectionReviewVm
    {
        public long ReviewId { get; set; }

        public long SectionId { get; set; }

        public string? SectionDecision { get; set; }

        public string? SectionComment { get; set; }

        public bool RequiresCorrection { get; set; }

        public bool RequiresInspection { get; set; }
    }
}
