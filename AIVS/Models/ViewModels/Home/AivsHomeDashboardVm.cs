namespace AIVS.Models.ViewModels.Home
{
    public class AivsHomeDashboardVm
    {
        public string DashboardTitle { get; set; } = "AIVS Dashboard";

        public string DashboardScope { get; set; } = string.Empty;

        public string? CurrentRole { get; set; }

        public string? CurrentSector { get; set; }

        public bool IsValuerDashboard { get; set; }

        public bool IsManagerDashboard { get; set; }

        public bool IsExecutiveDashboard { get; set; }

        public string? SelectedValuerUserId { get; set; }

        public int TotalSubmissions { get; set; }

        public int SectorInbox { get; set; }

        public int Claimed { get; set; }

        public int ValuerReview { get; set; }

        public int InspectionRequired { get; set; }

        public int InspectionConfirmed { get; set; }

        public int InspectionDetailsSent { get; set; }

        public int InspectionExpired { get; set; }

        public int ReturnedToClient { get; set; }

        public int ReadyForOvvioExtract { get; set; }

        public int SectorManagerQa { get; set; }

        public int SeniorManagerQa { get; set; }

        public int OvvioInserted { get; set; }

        public int Rejected { get; set; }

        public int Withdrawn { get; set; }

        public int CompletedThisMonth { get; set; }

        public List<DashboardTileVm> Tiles { get; set; } = new();

        public List<DashboardStatusCountVm> StatusCounts { get; set; } = new();

        public List<DashboardSectorStatsVm> SectorStats { get; set; } = new();

        public List<DashboardValuerStatsVm> ValuerStats { get; set; } = new();

        public List<DashboardRecentItemVm> RecentItems { get; set; } = new();
    }

    public class DashboardTileVm
    {
        public string Title { get; set; } = string.Empty;

        public int Count { get; set; }

        public string CssClass { get; set; } = string.Empty;
    }

    public class DashboardStatusCountVm
    {
        public string Status { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class DashboardSectorStatsVm
    {
        public string Sector { get; set; } = string.Empty;

        public int Total { get; set; }

        public int SectorInbox { get; set; }

        public int Assigned { get; set; }

        public int UnderReview { get; set; }

        public int Inspections { get; set; }

        public int ReadyForOvvio { get; set; }

        public int Rejected { get; set; }
    }

    public class DashboardValuerStatsVm
    {
        public string? ValuerUserId { get; set; }

        public string ValuerName { get; set; } = "Unassigned";

        public string? Sector { get; set; }

        public int TotalAssigned { get; set; }

        public int Claimed { get; set; }

        public int UnderReview { get; set; }

        public int InspectionRequired { get; set; }

        public int InspectionConfirmed { get; set; }

        public int InspectionExpired { get; set; }

        public int ReturnedToClient { get; set; }

        public int ReadyForOvvio { get; set; }

        public int Rejected { get; set; }
    }

    public class DashboardRecentItemVm
    {
        public long AttrId { get; set; }

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public string? Sector { get; set; }

        public string? ValuerName { get; set; }

        public string? Status { get; set; }

        public DateTime? LastUpdated { get; set; }
    }
}
