using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_ValuationDetails")]
    public class AttrValuationDetails
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(200)]
        public string? ValuationCategoryOnRoll { get; set; }

        [StringLength(200)]
        public string? ActualUse { get; set; }

        public bool IsMixedUse { get; set; }

        public string? AlternateUsages { get; set; }

        public string? OwnersTitleDeeds { get; set; }

        public string? OwnersFinancials { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey(nameof(PropertyDetailsId))]
        public AttrPropertyDetails? PropertyDetails { get; set; }
    }
}
