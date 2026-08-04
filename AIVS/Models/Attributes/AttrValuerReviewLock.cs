using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes;

[Table("AttrValuerReviewLocks", Schema = "dbo")]
public class AttrValuerReviewLock
{
    [Key] public long Id { get; set; }
    public long ReviewId { get; set; }
    public long Attr_ID { get; set; }
    public int UserId { get; set; }
    [StringLength(255)] public string UserName { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; } = DateTime.Now;
    public DateTime LastActivityAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
