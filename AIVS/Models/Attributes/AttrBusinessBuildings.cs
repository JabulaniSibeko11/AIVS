using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Business_Buildings")]
    public class AttrBusinessBuildings
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(50)]
        public string? BuildingNr { get; set; }

        [StringLength(50)]
        public string? Quality { get; set; }

        [StringLength(50)]
        public string? Condition { get; set; }

        public int? YearBuilt { get; set; }

        public int? Storeys { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Depreciation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GBA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DRC { get; set; }

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
