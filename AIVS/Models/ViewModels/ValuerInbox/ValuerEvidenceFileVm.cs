namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerEvidenceFileVm
    {
        public long Id { get; set; }

        public long AttrFileId { get; set; }

        public string? DisplayName { get; set; }

        public string? FileName { get; set; }

        // Original client file name. For client evidence this is never renamed by AIVS.
        public string? OriginalFileName { get; set; }

        public string? FilePath { get; set; }

        public string? EvidenceKey { get; set; }

        public string? FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTime? UploadedDateTime { get; set; }

        public string? UploadedBy { get; set; }
    }
}
