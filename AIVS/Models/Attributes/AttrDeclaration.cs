using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Declarations")]
    public class AttrDeclaration
    {
        [Key]
        public long ID { get; set; }

        public long Attr_ID { get; set; }

        [StringLength(100)]
        public string? Attr_No { get; set; }

        [StringLength(100)]
        public string? Attr_Ref_Signature { get; set; }

        public string? Declaration_Text { get; set; }

        public bool Declaration_Accepted { get; set; }

        public DateTime Declaration_Date { get; set; } = DateTime.Now;

        public string? Signature_Picture { get; set; }

        [StringLength(150)]
        public string? Signature_Name { get; set; }

        [StringLength(255)]
        public string? Signature_File_Name { get; set; }

        [StringLength(100)]
        public string? Signature_File_Type { get; set; }

        public string? Signature_File_Path { get; set; }

        [StringLength(50)]
        public string? RandomPin { get; set; }

        [StringLength(50)]
        public string? EvidencePin { get; set; }

        public DateTime PinGeneratedDateTime { get; set; } = DateTime.Now;

        public DateTime PinExpiryDateTime { get; set; }

        public bool PinIsActive { get; set; } = true;

        public int PinUsedCount { get; set; }

        public DateTime? LastPinUsedDateTime { get; set; }

        public bool AdditionalEvidenceAllowed { get; set; } = true;

        public DateTime AdditionalEvidenceDeadline { get; set; }

        [StringLength(100)]
        public string? DeclaredByUserId { get; set; }

        [StringLength(255)]
        public string? DeclaredByName { get; set; }

        [StringLength(255)]
        public string? DeclaredByEmail { get; set; }

        [StringLength(100)]
        public string? DeclaredByRole { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey(nameof(Attr_ID))]
        public AttrPropertyInfo? PropertyInfo { get; set; }
    }
}
