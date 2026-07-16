using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_DRC_MarketValueDemolition")]
    public class AttrDrcMarketValueDemolition
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DemolitionRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MarketValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MarketValueAfterDemolition { get; set; }

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
