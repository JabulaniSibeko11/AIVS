using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("LinkedProperties_Attr")]
    public class LinkedPropertyAttr
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string IDProperty { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UserID { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PropertyFrom { get; set; }
    }
}
