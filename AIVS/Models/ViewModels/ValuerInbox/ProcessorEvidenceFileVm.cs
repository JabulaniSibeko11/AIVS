namespace AIVS.Models.ViewModels.ValuerInbox;

public class ProcessorEvidenceFileVm
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? EvidenceComment { get; set; }
    public string? EvidenceStage { get; set; }
    public string? UploadedByName { get; set; }
    public string? UploadedByRole { get; set; }
    public DateTime UploadedAt { get; set; }
}
