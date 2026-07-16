using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_DRC_Improvements")]
    public class AttrDrcImprovements
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(250)]
        public string? ImprovementDescription { get; set; }

        [StringLength(50)]
        public string? Quality { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AreaUnit { get; set; }

        [StringLength(50)]
        public string? Condition { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DepreciationPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? RatePerSQM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DepreciatedRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ReplacementCost { get; set; }

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
