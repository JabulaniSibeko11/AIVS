using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Attr_Representatives")]
    public class AttrRepresentative
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public long? Attr_ID { get; set; }
        public string? Attr_No { get; set; }
        public string? IDProperty { get; set; }
        public string? UserID { get; set; }
        public string? Representative_Name { get; set; }
        public string? Rep_Postal_1 { get; set; }
        public string? Rep_Postal_2 { get; set; }
        public string? Rep_Postal_3 { get; set; }
        public string? Rep_Postal_4 { get; set; }
        public string? Rep_Postal_5 { get; set; }
        public string? Rep_Home_Phone { get; set; }
        public string? Rep_Cell_Phone { get; set; }
        public string? Rep_Work_Phone { get; set; }
        public string? Rep_Fax_Phone { get; set; }
        public string? Rep_Email { get; set; }
        public string? Auth_Letter_FileName { get; set; }
        public string? Auth_Letter_Path { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
