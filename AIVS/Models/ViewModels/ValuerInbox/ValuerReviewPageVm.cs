namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerReviewPageVm
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public long ReviewId { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Township { get; set; }

        public string? Sector { get; set; }

        public string? Status { get; set; }

        public string? FormType { get; set; }

        public int EvidenceCount { get; set; }

        public string? ReviewStatus { get; set; }

        public DateTime StartedAt { get; set; }
        public bool CanSubmitToOvvio { get; set; }

        public bool HasRequiredCorrections { get; set; }

        public bool HasInspectionRequired { get; set; }

        public int TotalSections { get; set; }

        public int ReviewedSections { get; set; }

        public int CorrectionSections { get; set; }

        public int InspectionSections { get; set; }

        public string? SubmitBlockReason { get; set; }

        public string? FinalDecision { get; set; }

        public string? FinalComment { get; set; }
        public string? ValuerEvidencePath { get; set; }
        public List<ValuerEvidenceFileVm> EvidenceFiles { get; set; } = new();
        public AttributeSubmissionViewModel? SubmittedForm { get; set; }

        public List<ValuerReviewSectionVm> Sections { get; set; } = new();
        public InspectionRequestVm? ActiveInspectionRequest { get; set; }
        public List<ValuerPhysicalInspectionEvidenceVm> PhysicalInspectionEvidenceFiles { get; set; } = new();
        public int PhysicalInspectionEvidenceCount => PhysicalInspectionEvidenceFiles?.Count ?? 0;
        public List<AttributeComparisonSectionVm> ComparisonSections { get; set; } = new();
        public bool HasCityData { get; set; }
        public int DifferenceCount => ComparisonSections.Sum(x => x.Fields.Count(f => f.HasDifference));
        public int MatchingCount => ComparisonSections.Sum(x => x.Fields.Count(f => !f.HasDifference && !f.IsReadOnly));
        public int MissingCityCount => ComparisonSections.Sum(x => x.Fields.Count(f => string.IsNullOrWhiteSpace(f.CityValue)));
        public string ActiveTab { get; set; } = "1";
        public bool DifferencesOnly { get; set; } = true;
        public bool IsLockedByAnotherUser { get; set; }
        public string? LockedByName { get; set; }
        public DateTime? LockExpiresAt { get; set; }
        public List<AttributeAuditEventVm> AuditTimeline { get; set; } = new();
    }
}
