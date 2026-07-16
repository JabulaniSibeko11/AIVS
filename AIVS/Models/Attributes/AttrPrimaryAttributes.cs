using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_PrimaryAttributes")]
    public class AttrPrimaryAttributes
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tla1 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tla2 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tla3 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Garage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CarportCp { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GrannyFlatGf { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? StaffQuartersSq { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Storage { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? AdjustmentFactor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? STMain { get; set; }

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
