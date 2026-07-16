using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Calculations")]
    public class AttrCalculations
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(100)]
        public string? CalcUpdateTla { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tla { get; set; }

        [StringLength(100)]
        public string? CalcUpdateWgba { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? AdjustedWgba { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalValueNonRes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalValueUnutilisedLand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DRCFinalValue { get; set; }

        [StringLength(100)]
        public string? CalculationStatus { get; set; }

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
