namespace AIVS.Models.Attributes
{
    public class AttrInspectionEvidence
    {
        public long Id { get; set; }

        public long Attr_ID { get; set; }

        public string? Attr_No { get; set; }

        public long? InspectionRequestId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public long? FileSizeBytes { get; set; }

        public string? UploadedBySapNumber { get; set; }

        public string? UploadedByUserId { get; set; }

        public string? UploadedByName { get; set; }

        public string? CaptureSource { get; set; }

        public string? EvidenceComment { get; set; }

        public DateTime UploadedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
