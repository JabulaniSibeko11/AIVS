using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_PropertyDetails")]
    public class AttrPropertyDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FormType { get; set; } = string.Empty;

        [StringLength(150)]
        public string? HArea { get; set; }

        [StringLength(150)]
        public string? DataController { get; set; }

        [StringLength(150)]
        public string? CollectionBlock { get; set; }

        [StringLength(150)]
        public string? DataCollector { get; set; }

        [StringLength(100)]
        public string? SGNumber { get; set; }

        [StringLength(200)]
        public string? Centroid { get; set; }

        [StringLength(200)]
        public string? Erf { get; set; }

        [StringLength(100)]
        public string? Extent { get; set; }

        [StringLength(200)]
        public string? SectionalTitle { get; set; }

        [StringLength(200)]
        public string? LandUseFinancials { get; set; }

        [StringLength(200)]
        public string? Municipality { get; set; }

        [StringLength(50)]
        public string? Ward { get; set; }

        [StringLength(200)]
        public string? Township { get; set; }

        [StringLength(300)]
        public string? Zoning { get; set; }

        [StringLength(500)]
        public string? Sources { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public AttrValuationDetails? ValuationDetails { get; set; }
        public AttrAccess? Access { get; set; }
        public AttrPrimaryAttributes? PrimaryAttributes { get; set; }
        public AttrSecondaryAttributes? SecondaryAttributes { get; set; }
        public AttrCalculations? Calculations { get; set; }
        public AttrBusinessGeneral? BusinessGeneral { get; set; }
        public AttrDrcMarketValueDemolition? DrcMarketValueDemolition { get; set; }

        public ICollection<AttrContactInfo> ContactInfos { get; set; } = new List<AttrContactInfo>();
        public ICollection<AttrBusinessBuildings> BusinessBuildings { get; set; } = new List<AttrBusinessBuildings>();
        public ICollection<AttrBusinessSections> BusinessSections { get; set; } = new List<AttrBusinessSections>();
        public ICollection<AttrDrcBuildings> DrcBuildings { get; set; } = new List<AttrDrcBuildings>();
        public ICollection<AttrDrcImprovements> DrcImprovements { get; set; } = new List<AttrDrcImprovements>();
        public ICollection<AttrDrcVacantLand> DrcVacantLands { get; set; } = new List<AttrDrcVacantLand>();
        public ICollection<AttrPropertyInfo> PropertyInfos { get; set; } = new List<AttrPropertyInfo>();
    }
}
