using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes;

[Table("AttrValuerReviewFieldResolutions", Schema = "dbo")]
public class AttrValuerReviewFieldResolution
{
    [Key] public long Id { get; set; }
    public long ReviewId { get; set; }
    public long Attr_ID { get; set; }
    [StringLength(100)] public string SectionCode { get; set; } = string.Empty;
    [StringLength(100)] public string FieldCode { get; set; } = string.Empty;
    [StringLength(200)] public string FieldLabel { get; set; } = string.Empty;
    public string? CityValue { get; set; }
    public string? ClientValue { get; set; }
    [StringLength(30)] public string Decision { get; set; } = string.Empty; // AcceptClient / KeepCity
    public string? ResolvedValue { get; set; }
    public bool IsActive { get; set; } = true;
    public int ResolvedByUserId { get; set; }
    [StringLength(255)] public string? ResolvedByName { get; set; }
    public DateTime ResolvedAt { get; set; } = DateTime.Now;
}
