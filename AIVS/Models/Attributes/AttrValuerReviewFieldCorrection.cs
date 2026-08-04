using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes;

[Table("AttrValuerReviewFieldCorrections", Schema = "dbo")]
public class AttrValuerReviewFieldCorrection
{
    [Key] public long Id { get; set; }
    public long ReviewId { get; set; }
    public long Attr_ID { get; set; }
    [StringLength(100)] public string SectionCode { get; set; } = string.Empty;
    [StringLength(100)] public string FieldCode { get; set; } = string.Empty;
    [StringLength(200)] public string FieldLabel { get; set; } = string.Empty;
    public string? CityValue { get; set; }
    public string? ClientValue { get; set; }
    public bool IsActive { get; set; } = true;
    public int SelectedByUserId { get; set; }
    [StringLength(255)] public string? SelectedByName { get; set; }
    public DateTime SelectedAt { get; set; } = DateTime.Now;
}
