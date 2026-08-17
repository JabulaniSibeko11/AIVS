namespace AIVS.Models.ViewModels.ValuerInbox;

public class SaveReviewDraftVm
{
    public long ReviewId { get; set; }
    public long AttrId { get; set; }
    public string ActiveTab { get; set; } = "1";
    public string? ValuerComment { get; set; }
    public bool DifferencesOnly { get; set; }
}

public class SaveCorrectionFieldsVm
{
    public long ReviewId { get; set; }
    public long AttrId { get; set; }
    public List<string> FieldKeys { get; set; } = new();
}

public class QuickSectionDecisionVm
{
    public long ReviewId { get; set; }
    public long AttrId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
}

public class ResolveRatingDifferenceVm
{
    public long ReviewId { get; set; }
    public long AttrId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string FieldCode { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
}
