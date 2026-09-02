
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("AttrInspectionCalendarBlockArchive", Schema = "dbo")]
    public class AttrInspectionCalendarBlockArchive
    {
        [Key]
        public long Id { get; set; }

        public long OriginalBlockId { get; set; }
        public int UserId { get; set; }

        public DateTime BlockedFrom { get; set; }
        public DateTime BlockedTo { get; set; }

        public bool IsWholeDay { get; set; }

        [StringLength(250)]
        public string? Reason { get; set; }

        public bool SourceIsActive { get; set; }

        [StringLength(150)]
        public string? OriginalCreatedBy { get; set; }

        public DateTime? OriginalCreatedDate { get; set; }

        [StringLength(150)]
        public string? OriginalUpdatedBy { get; set; }

        public DateTime? OriginalUpdatedDate { get; set; }

        public DateTime ArchivedDate { get; set; } = DateTime.Now;

        public DateTime ArchiveBatchMonth { get; set; }
    }
}
