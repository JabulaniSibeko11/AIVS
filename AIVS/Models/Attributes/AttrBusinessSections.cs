using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Business_Sections")]
    public class AttrBusinessSections
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(50)]
        public string? BuildingNr { get; set; }

        [StringLength(150)]
        public string? Usage { get; set; }

        [StringLength(150)]
        public string? MarketGroup { get; set; }

        [StringLength(50)]
        public string? Quality { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GBA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NLA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Rental { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Vac { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Exp { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Gross { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Normalised { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Nett { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Value { get; set; }

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
