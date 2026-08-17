using AIVS.Models.ViewModels.ValuerInbox;

namespace AIVS.Models.ViewModels.SectorManager
{
    public class SectorManagerQaDetailsVm
    {
        public long QaId { get; set; }

        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public long? ValuerReviewId { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Township { get; set; }

        public string? Sector { get; set; }

        public string? CurrentStatus { get; set; }

        public string? QaStatus { get; set; }

        public string? SectorManagerName { get; set; }

        public string? SectorManagerComment { get; set; }

        public DateTime? SectorManagerCompletedAt { get; set; }

        public string? SeniorQaStatus { get; set; }

        public string? SelectionReason { get; set; }

        public DateTime QaWeekStartDate { get; set; }

        public DateTime QaWeekEndDate { get; set; }

        public string? ValuerName { get; set; }

        public DateTime? ValuerSubmittedAt { get; set; }

        public string? ValuerFinalDecision { get; set; }

        public string? ValuerFinalComment { get; set; }

        public string? ReviewedPdfPathBeforeQa { get; set; }

        public string? ReviewedPdfPathAfterQa { get; set; }

        public AttributeSubmissionViewModel? SubmittedForm { get; set; }

        public List<ValuerReviewSectionVm> Sections { get; set; } = new();

        public List<ValuerEvidenceFileVm> EvidenceFiles { get; set; } = new();

        public List<ValuerPhysicalInspectionEvidenceVm> PhysicalInspectionEvidenceFiles { get; set; } = new();

        public List<ProcessorEvidenceFileVm> ProcessorEvidenceFiles { get; set; } = new();

        public ValuerReviewPageVm? ReviewPage { get; set; }

        public string? SeniorManagerName { get; set; }

        public string? SeniorManagerComment { get; set; }

        public bool SectorManagerWasProcessor { get; set; }

        public int TotalSections => Sections?.Count ?? 0;

        public int ReviewedSections => Sections?.Count(x => !string.IsNullOrWhiteSpace(x.SectionDecision)) ?? 0;

        public int CorrectionSections => Sections?.Count(x =>
            x.RequiresCorrection ||
            x.SectionDecision == "Needs correction") ?? 0;

        public int InspectionSections => Sections?.Count(x =>
            x.RequiresInspection ||
            x.SectionDecision == "Requires inspection") ?? 0;

        public bool CanApprove =>
            string.Equals(QaStatus, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(QaStatus, "InProgress", StringComparison.OrdinalIgnoreCase);
    }
}
