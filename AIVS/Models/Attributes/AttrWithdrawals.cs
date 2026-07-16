using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Withdrawals")]
    public class AttrWithdrawals
    {
        [Key]
        public long ID_Withdrawal { get; set; }

        public long Attr_ID { get; set; }

        [StringLength(100)]
        public string? Attr_No { get; set; }

        [Required]
        [StringLength(100)]
        public string Attribute_Withdrawn { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? WithdrawalReason { get; set; }

        [StringLength(100)]
        public string? WithdrawnByUserId { get; set; }

        [StringLength(255)]
        public string? WithdrawnByName { get; set; }

        [StringLength(100)]
        public string? WithdrawnByRole { get; set; }

        [Required]
        [StringLength(50)]
        public string WithdrawalStatus { get; set; } = "Withdrawn";

        public DateTime DateWithdrawn { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? ProcessedBy { get; set; }

        public DateTime? ProcessedDate { get; set; }

        [StringLength(1000)]
        public string? ProcessingComment { get; set; }

        [ForeignKey(nameof(Attr_ID))]
        public AttrPropertyInfo? PropertyInfo { get; set; }
    }
}
