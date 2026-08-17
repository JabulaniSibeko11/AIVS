using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes;

[Table("Attr_OvvioApprovedAttributes", Schema = "dbo")]
public class AttrOvvioApprovedAttribute
{
    [Key]
    public long Id { get; set; }
    public long Attr_ID { get; set; }
    [MaxLength(100)] public string? Attr_No { get; set; }
    [MaxLength(100)] public string? PremiseId { get; set; }
    [MaxLength(100)] public string? ValuationKey { get; set; }
    [MaxLength(255)] public string? PropertyDescription { get; set; }
    [MaxLength(255)] public string? Township { get; set; }
    [MaxLength(100)] public string? Sector { get; set; }
    [MaxLength(100)] public string? PropertyType { get; set; }
    public string ApprovedAttributeJson { get; set; } = "{}";
    [MaxLength(50)] public string ExportStatus { get; set; } = "Inserted";
    public DateTime ApprovedAt { get; set; }
    [MaxLength(255)] public string? ApprovedBy { get; set; }
    [MaxLength(100)] public string? ApprovedByUserId { get; set; }
    public string? ApprovalComment { get; set; }
    public string? ApprovalNoticePath { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    [MaxLength(100)] public string? CreatedBy { get; set; }
}
