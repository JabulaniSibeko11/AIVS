using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_CityAttributeValues", Schema = "dbo")]
    public class AttrCityAttributeValue
    {
        [Key]
        public long Id { get; set; }

        [Required, StringLength(100)]
        public string PremiseId { get; set; } = string.Empty;

        [StringLength(50)]
        public string? FormType { get; set; }

        [Required, StringLength(100)]
        public string SectionCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FieldCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string FieldLabel { get; set; } = string.Empty;

        public string? FieldValue { get; set; }

        public int DisplayOrder { get; set; }

        [StringLength(100)]
        public string? SourceSystem { get; set; }

        public DateTime? SourceEffectiveDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
