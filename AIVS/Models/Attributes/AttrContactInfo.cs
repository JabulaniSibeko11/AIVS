using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_ContactInfo")]
    public class AttrContactInfo
    {
        [Key]
        public int Id { get; set; }

        public int PropertyDetailsId { get; set; }

        [StringLength(50)]
        public string? ContactType { get; set; }

        public bool IsCompany { get; set; }

        [StringLength(250)]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        public string? CompanyRegistrationNumber { get; set; }

        [StringLength(200)]
        public string? FirstNames { get; set; }

        [StringLength(200)]
        public string? LastName { get; set; }

        [StringLength(200)]
        public string? MaidenName { get; set; }

        [StringLength(100)]
        public string? IDNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? MaritalStatus { get; set; }

        [StringLength(50)]
        public string? Citizenship { get; set; }

        public string? PhysicalAddress { get; set; }

        public string? PostalAddress { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? HomePhoneNo { get; set; }

        [StringLength(50)]
        public string? WorkPhoneNo { get; set; }

        [StringLength(50)]
        public string? CellNo { get; set; }

        [StringLength(50)]
        public string? FaxNo { get; set; }

        public bool? Interviewed { get; set; }

        public string? Comments { get; set; }

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
