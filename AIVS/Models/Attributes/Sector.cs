using System.ComponentModel.DataAnnotations.Schema;

namespace AIVS.Models.Attributes
{
    [Table("Sectors")]
    public class Sector
    {
        public string? TOWN_NAME_DESC { get; set; }

        public string? SECTOR { get; set; }
    }
}
