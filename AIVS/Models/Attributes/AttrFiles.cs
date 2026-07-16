using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Files")]
    public class AttrFiles
    {
        [Key]
        public long ID { get; set; }

        public long? Attr_ID { get; set; }

        [StringLength(100)] public string? Attr_No { get; set; }
        [StringLength(100)] public string? Attr_Ref_Files { get; set; }

        [StringLength(255)] public string? Files1 { get; set; }
        [StringLength(255)] public string? Files2 { get; set; }
        [StringLength(255)] public string? Files3 { get; set; }
        [StringLength(255)] public string? Files4 { get; set; }
        [StringLength(255)] public string? Files5 { get; set; }
        [StringLength(255)] public string? Files6 { get; set; }
        [StringLength(255)] public string? Files7 { get; set; }
        [StringLength(255)] public string? Files8 { get; set; }
        [StringLength(255)] public string? Files9 { get; set; }
        [StringLength(255)] public string? Files10 { get; set; }
        [StringLength(255)] public string? Rep_Letter { get; set; }

        // ── These ARE real computed columns in the DB ──────────────
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Acknowledgement_FileName { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? RelativeFolder { get; set; }

        public int? Evidence_Count { get; set; }
        [StringLength(500)] public string? RootFolder { get; set; }
        [StringLength(255)] public string? Bulk_File_Name { get; set; }

        [StringLength(100)] public string? UploadedByUserId { get; set; }
        [StringLength(255)] public string? UploadedByName { get; set; }
        [StringLength(100)] public string? UploadedByRole { get; set; }

        public DateTime UploadedDateTime { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        [StringLength(100)] public string? DeletedByUserId { get; set; }
        [StringLength(255)] public string? DeletedByName { get; set; }
        public DateTime? DeletedDateTime { get; set; }
        [StringLength(1000)] public string? DeleteReason { get; set; }

        [StringLength(100)] public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [StringLength(100)] public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // ── NOT mapped — no DB columns, computed in C# ────────────
        [NotMapped] public string? Files1_Path => Join(Files1);
        [NotMapped] public string? Files2_Path => Join(Files2);
        [NotMapped] public string? Files3_Path => Join(Files3);
        [NotMapped] public string? Files4_Path => Join(Files4);
        [NotMapped] public string? Files5_Path => Join(Files5);
        [NotMapped] public string? Files6_Path => Join(Files6);
        [NotMapped] public string? Files7_Path => Join(Files7);
        [NotMapped] public string? Files8_Path => Join(Files8);
        [NotMapped] public string? Files9_Path => Join(Files9);
        [NotMapped] public string? Files10_Path => Join(Files10);
        [NotMapped] public string? RepLetter_Path => Join(Rep_Letter);
        [NotMapped] public string? Acknowledgement_Path => Join(Acknowledgement_FileName);
        [NotMapped] public string? Bulk_File_Path => Join(Bulk_File_Name);

        private string? Join(string? fileName)
            => string.IsNullOrWhiteSpace(fileName) ||
               string.IsNullOrWhiteSpace(RootFolder)
                ? null
                : Path.Combine(RootFolder, fileName);

        [ForeignKey(nameof(Attr_ID))]
        public AttrPropertyInfo? PropertyInfo { get; set; }
    }
}