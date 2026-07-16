namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerReviewSectionVm
    {
        public long SectionId { get; set; }

        public string SectionCode { get; set; } = string.Empty;

        public string SectionName { get; set; } = string.Empty;

        public string? SectionDecision { get; set; }

        public string? SectionComment { get; set; }

        public bool RequiresCorrection { get; set; }

        public bool RequiresInspection { get; set; }
    }
}
