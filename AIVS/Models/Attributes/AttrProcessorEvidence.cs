namespace AIVS.Models.Attributes;

public class AttrProcessorEvidence
{
    public long Id { get; set; }
    public long Attr_ID { get; set; }
    public string? Attr_No { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? EvidenceComment { get; set; }
    public string? EvidenceStage { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? UploadedByName { get; set; }
    public string? UploadedByRole { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
