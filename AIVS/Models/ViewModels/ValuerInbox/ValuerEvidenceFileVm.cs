namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerEvidenceFileVm
    {
        public long Id { get; set; }

        public long AttrFileId { get; set; }

        public string? DisplayName { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? FileType { get; set; }

        public long? FileSize { get; set; }

        public DateTime? UploadedDateTime { get; set; }

        public string? UploadedBy { get; set; }
    }
}
