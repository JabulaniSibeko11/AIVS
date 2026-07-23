namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerPhysicalInspectionEvidenceVm
    {
        public long Id { get; set; }

        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public long? InspectionRequestId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string? ContentType { get; set; }

        public long? FileSizeBytes { get; set; }

        public string? UploadedBySapNumber { get; set; }

        public string? UploadedByName { get; set; }

        public string? CaptureSource { get; set; }

        public string? EvidenceComment { get; set; }

        public DateTime UploadedAt { get; set; }

        public bool IsImage =>
            ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ||
            FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes == null || FileSizeBytes <= 0)
                    return "-";

                var mb = FileSizeBytes.Value / 1024m / 1024m;
                return $"{mb:N2} MB";
            }
        }
    }
}