using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_DRC_VacantLand")]
    public class AttrDrcVacantLand
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(150)]
        public string? Region { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinRatePerSQM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MidRatePerSQM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxRatePerSQM { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Area { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Rate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? VacantLandCost { get; set; }

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
