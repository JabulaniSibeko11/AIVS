using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes;

[Table("AttrValuerReviewDrafts", Schema = "dbo")]
public class AttrValuerReviewDraft
{
    [Key] public long Id { get; set; }
    public long ReviewId { get; set; }
    public long Attr_ID { get; set; }
    public int UserId { get; set; }
    [StringLength(20)] public string ActiveTab { get; set; } = "1";
    public string? ValuerComment { get; set; }
    public bool DifferencesOnly { get; set; } = true;
    public DateTime SavedAt { get; set; } = DateTime.Now;
}
