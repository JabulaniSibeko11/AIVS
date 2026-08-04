namespace AIVS.Models.ViewModels.SeniorManager;

public class SeniorManagerQaInboxItemVm
{
    public long QaId { get; set; }
    public long AttrId { get; set; }
    public string? AttrNo { get; set; }
    public string? PropertyDescription { get; set; }
    public string? Township { get; set; }
    public string? Sector { get; set; }
    public string? ValuerName { get; set; }
    public string? SectorManagerName { get; set; }
    public DateTime? SectorManagerCompletedAt { get; set; }
    public string? SeniorQaStatus { get; set; }
    public int? SeniorManagerUserId { get; set; }
    public string? SeniorManagerName { get; set; }
    public bool CanClaim { get; set; }
    public bool IsAssignedToMe { get; set; }
}
