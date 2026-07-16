using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_SecondaryAttributes")]
    public class AttrSecondaryAttributes
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        public int? Storeys { get; set; }

        [StringLength(50)]
        public string? Security { get; set; }

        [StringLength(100)]
        public string? Noise { get; set; }

        [StringLength(50)]
        public string? Topography { get; set; }

        [StringLength(50)]
        public string? Quality { get; set; }

        [StringLength(50)]
        public string? Condition { get; set; }

        public bool? SwimmingPool { get; set; }

        public bool? TennisCourt { get; set; }

        public int? STCondition { get; set; }

        public int? STFloor { get; set; }

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
