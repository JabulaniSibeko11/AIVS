using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Property_Info_AuditTrail")]
    public class AttrPropertyInfoAuditTrail
    {
        [Key]
        public long Audit_ID { get; set; }

        public long Attr_ID { get; set; }

        [StringLength(100)]
        public string? Attr_No { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        public string? OldStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [StringLength(100)]
        public string? ActionByUserId { get; set; }

        [StringLength(255)]
        public string? ActionByName { get; set; }

        [StringLength(100)]
        public string? ActionRole { get; set; }

        public string? Comment { get; set; }

        public DateTime ActionDateTime { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? IPAddress { get; set; }

        [StringLength(255)]
        public string? MachineName { get; set; }

        [ForeignKey(nameof(Attr_ID))]
        public AttrPropertyInfo? PropertyInfo { get; set; }
    }
}
