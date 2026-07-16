namespace AIVS.Models.ViewModels.ValuerInbox
{
    public class ValuerEvidenceFileVm
    {
        public long Id { get; set; }

        public string? FileName { get; set; }

        public string? FilePath { get; set; }

        public string? FileType { get; set; }

        public DateTime? UploadedDate { get; set; }

        public string? UploadedBy { get; set; }
    }
}
