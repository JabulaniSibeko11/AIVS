namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class AttributeComparisonSectionVm
    {
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public int TabNumber { get; set; }
        public int DisplayOrder { get; set; }
        public List<AttributeComparisonFieldVm> Fields { get; set; } = new();
    }

    public class AttributeComparisonFieldVm
    {
        public string FieldCode { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string? CityValue { get; set; }
        public string? ClientValue { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsReadOnly { get; set; }
        public bool HasCityValue { get; set; }
        public bool HasDifference { get; set; }
        public bool IsSelectedForCorrection { get; set; }
        public bool CanResolveRatingDifference { get; set; }
        public bool IsResolved { get; set; }
        public string? ResolutionDecision { get; set; }
        public string? ResolvedValue { get; set; }
    }

    public class AttributeAuditEventVm
    {
        public string Action { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ActionBy { get; set; }
        public string? Comment { get; set; }
        public DateTime ActionDateTime { get; set; }
    }
}
