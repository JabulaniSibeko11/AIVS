using System.ComponentModel.DataAnnotations;

namespace AIVS.Models.ViewModels.SeniorManager;

public class SeniorManagerQaDecisionVm
{
    public long QaId { get; set; }
    public long AttrId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
