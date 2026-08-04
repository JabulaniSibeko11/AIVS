using AIVS.Data;
using AIVS.Models.ViewModels.Home;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using AIVS.Security;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Implementations
{
    public class HomeDashboardService : IHomeDashboardService
    {
        private readonly AttributesDbContext _context;
        private readonly IAivsRoleAccessService _roleAccess;

        public HomeDashboardService(AttributesDbContext context, IAivsRoleAccessService roleAccess)
        {
            _context = context;
            _roleAccess = roleAccess;
        }

        public async Task<AivsHomeDashboardVm> BuildDashboardAsync(
            AivsCurrentUserVm currentUser,
            string? selectedValuerUserId)
        {
            var sector = Clean(currentUser.Sector);
            var userId = currentUser.UserId?.ToString();

            var isValuer = _roleAccess.IsValuer(currentUser);
            var isManager = _roleAccess.IsSectorManager(currentUser) || _roleAccess.IsSeniorManager(currentUser);
            var isExecutive = _roleAccess.IsLeadership(currentUser) || _roleAccess.IsAdministrator(currentUser);

            var query = _context.AttrPropertyInfo
                .AsNoTracking()
                .Where(x => x.IsActive == true);

            if (isValuer)
            {
                query = query.Where(x =>
                    x.Task_Assigned_To_UserId == userId ||
                    x.ValuerUserId == userId);
            }
            else if (isManager)
            {
                if (string.IsNullOrWhiteSpace(sector))
                {
                    query = query.Where(x => false);
                }
                else
                {
                    query = query.Where(x =>
                        x.RoutedSector != null &&
                        x.RoutedSector.Trim().ToUpper() == sector);
                }
            }
            else if (!isExecutive)
            {
                query = query.Where(x => false);
            }

            var rows = await query
                .Select(x => new HomeDashboardRow
                {
                    AttrId = x.Attr_ID,
                    AttrNo = x.Attr_No,
                    PropertyDescription = x.Property_Desc,
                    Sector = x.RoutedSector ?? x.Sector,
                    Status = x.Attr_Status,
                    TaskAssignedTo = x.Task_Assigned_To,
                    TaskAssignedToUserId = x.Task_Assigned_To_UserId,
                    Valuer = x.Valuer,
                    ValuerUserId = x.ValuerUserId,
                    SubmissionDateTime = x.SubmissionDateTime,
                    UpdatedDate = x.UpdatedDate,
                    ValuerDecisionDateTime = x.ValuerDecisionDateTime,
                    IsWithdrawn = x.IsWithdrawn
                })
                .ToListAsync();

            var allRowsForValuerTable = rows;

            if (!isValuer && !string.IsNullOrWhiteSpace(selectedValuerUserId))
            {
                rows = rows
                    .Where(x =>
                        x.TaskAssignedToUserId == selectedValuerUserId ||
                        x.ValuerUserId == selectedValuerUserId)
                    .ToList();
            }

            var model = new AivsHomeDashboardVm
            {
                CurrentRole = currentUser.Role,
                CurrentSector = currentUser.Sector,
                SelectedValuerUserId = selectedValuerUserId,
                IsValuerDashboard = isValuer,
                IsManagerDashboard = isManager,
                IsExecutiveDashboard = isExecutive
            };

            if (isValuer)
            {
                model.DashboardTitle = "My Valuer Dashboard";
                model.DashboardScope = currentUser.FullName ?? "Valuer";
            }
            else if (isManager)
            {
                model.DashboardTitle = _roleAccess.IsSeniorManager(currentUser)
                    ? "Senior Manager Dashboard"
                    : "Sector Manager Dashboard";
                model.DashboardScope = string.IsNullOrWhiteSpace(currentUser.Sector)
                    ? "No sector linked to your profile"
                    : $"Sector: {currentUser.Sector}";
            }
            else if (isExecutive)
            {
                model.DashboardTitle = _roleAccess.NormalizeRole(currentUser.Role) == "DEPUTY DIRECTOR"
                    ? "Deputy Director Dashboard"
                    : _roleAccess.IsAdministrator(currentUser)
                        ? "AIVS Administration Dashboard"
                        : "Executive System Dashboard";
                model.DashboardScope = "All sectors";
            }
            else
            {
                model.DashboardTitle = "AIVS Dashboard";
                model.DashboardScope = "No dashboard access for this role";
            }

            PopulateMainCounts(model, rows);
            PopulateTiles(model);
            PopulateStatusCounts(model, rows);
            PopulateSectorStats(model, rows);
            PopulateValuerStats(model, allRowsForValuerTable);
            PopulateRecentItems(model, rows);

            return model;
        }
        private static void PopulateMainCounts(
            AivsHomeDashboardVm model,
            List<HomeDashboardRow> rows)
        {
            model.TotalSubmissions = rows.Count;

            model.SectorInbox = CountStatus(rows, "SectorInbox");
            model.Claimed = CountStatus(rows, "Claimed");
            model.ValuerReview = CountStatus(rows, "ValuerReview");

            model.InspectionRequired = CountStatus(rows, "InspectionRequired");
            model.InspectionConfirmed = CountStatus(rows, "InspectionConfirmed");
            model.InspectionDetailsSent = CountStatus(rows, "InspectionDetailsSent");
            model.InspectionExpired = CountStatus(rows, "InspectionExpired");

            model.ReturnedToClient = CountStatus(rows, "ReturnedToClient");
            model.ReadyForOvvioExtract = CountStatus(rows, "ReadyForOvvioExtract");
            model.SeniorManagerQa = CountStatus(rows, "SeniorManagerQa");
            model.Rejected = CountStatus(rows, "Rejected");

            model.Withdrawn = rows.Count(x => x.IsWithdrawn);

            var firstDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            model.CompletedThisMonth = rows.Count(x =>
                x.ValuerDecisionDateTime != null &&
                x.ValuerDecisionDateTime >= firstDay);
        }

        private static void PopulateTiles(AivsHomeDashboardVm model)
        {
            model.Tiles = new List<DashboardTileVm>
            {
                new() { Title = "Total", Count = model.TotalSubmissions, CssClass = "tile-total" },
                new() { Title = "Sector Inbox", Count = model.SectorInbox, CssClass = "tile-inbox" },
                new() { Title = "Claimed", Count = model.Claimed, CssClass = "tile-claimed" },
                new() { Title = "Under Review", Count = model.ValuerReview, CssClass = "tile-review" },
                new() { Title = "Inspection Required", Count = model.InspectionRequired, CssClass = "tile-inspection" },
                new() { Title = "Inspection Confirmed", Count = model.InspectionConfirmed, CssClass = "tile-confirmed" },
                new() { Title = "Ready for OVVIO", Count = model.ReadyForOvvioExtract, CssClass = "tile-ovvio" },
                new() { Title = "Senior Manager QA", Count = model.SeniorManagerQa, CssClass = "tile-review" },
                new() { Title = "Returned", Count = model.ReturnedToClient, CssClass = "tile-returned" },
                new() { Title = "Rejected", Count = model.Rejected, CssClass = "tile-rejected" }
            };
        }

        private static void PopulateStatusCounts(
            AivsHomeDashboardVm model,
            List<HomeDashboardRow> rows)
        {
            model.StatusCounts = rows
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Status) ? "Unknown" : x.Status.Trim())
                .Select(g => new DashboardStatusCountVm
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        private static void PopulateSectorStats(
            AivsHomeDashboardVm model,
            List<HomeDashboardRow> rows)
        {
            model.SectorStats = rows
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Sector) ? "Unknown" : x.Sector.Trim())
                .Select(g => new DashboardSectorStatsVm
                {
                    Sector = g.Key,
                    Total = g.Count(),
                    SectorInbox = g.Count(x => IsStatus(x, "SectorInbox")),
                    Assigned = g.Count(x => !string.IsNullOrWhiteSpace(x.TaskAssignedToUserId)),
                    UnderReview = g.Count(x =>
                        IsStatus(x, "Claimed") ||
                        IsStatus(x, "ValuerReview")),
                    Inspections = g.Count(x =>
                        IsStatus(x, "InspectionRequired") ||
                        IsStatus(x, "InspectionConfirmed") ||
                        IsStatus(x, "InspectionDetailsSent") ||
                        IsStatus(x, "InspectionExpired")),
                    ReadyForOvvio = g.Count(x => IsStatus(x, "ReadyForOvvioExtract")),
                    Rejected = g.Count(x => IsStatus(x, "Rejected"))
                })
                .OrderBy(x => x.Sector)
                .ToList();
        }

        private static void PopulateValuerStats(
            AivsHomeDashboardVm model,
            List<HomeDashboardRow> rows)
        {
            var assignedRows = rows
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.TaskAssignedToUserId) ||
                    !string.IsNullOrWhiteSpace(x.ValuerUserId))
                .ToList();

            model.ValuerStats = assignedRows
                .GroupBy(x => new
                {
                    UserId = !string.IsNullOrWhiteSpace(x.ValuerUserId)
                        ? x.ValuerUserId
                        : x.TaskAssignedToUserId,

                    Name = !string.IsNullOrWhiteSpace(x.Valuer)
                        ? x.Valuer
                        : x.TaskAssignedTo,

                    Sector = x.Sector
                })
                .Select(g => new DashboardValuerStatsVm
                {
                    ValuerUserId = g.Key.UserId,
                    ValuerName = string.IsNullOrWhiteSpace(g.Key.Name) ? "Unknown Valuer" : g.Key.Name!,
                    Sector = g.Key.Sector,
                    TotalAssigned = g.Count(),
                    Claimed = g.Count(x => IsStatus(x, "Claimed")),
                    UnderReview = g.Count(x => IsStatus(x, "ValuerReview")),
                    InspectionRequired = g.Count(x => IsStatus(x, "InspectionRequired")),
                    InspectionConfirmed = g.Count(x =>
                        IsStatus(x, "InspectionConfirmed") ||
                        IsStatus(x, "InspectionDetailsSent")),
                    InspectionExpired = g.Count(x => IsStatus(x, "InspectionExpired")),
                    ReturnedToClient = g.Count(x => IsStatus(x, "ReturnedToClient")),
                    ReadyForOvvio = g.Count(x => IsStatus(x, "ReadyForOvvioExtract")),
                    Rejected = g.Count(x => IsStatus(x, "Rejected"))
                })
                .OrderBy(x => x.ValuerName)
                .ToList();
        }

        private static void PopulateRecentItems(
            AivsHomeDashboardVm model,
            List<HomeDashboardRow> rows)
        {
            model.RecentItems = rows
                .OrderByDescending(x => x.UpdatedDate ?? x.SubmissionDateTime)
                .Take(10)
                .Select(x => new DashboardRecentItemVm
                {
                    AttrId = x.AttrId,
                    AttrNo = x.AttrNo,
                    PropertyDescription = x.PropertyDescription,
                    Sector = x.Sector,
                    ValuerName = !string.IsNullOrWhiteSpace(x.Valuer)
                        ? x.Valuer
                        : x.TaskAssignedTo,
                    Status = x.Status,
                    LastUpdated = x.UpdatedDate ?? x.SubmissionDateTime
                })
                .ToList();
        }

        private static int CountStatus(List<HomeDashboardRow> rows, string status)
        {
            return rows.Count(x => IsStatus(x, status));
        }

        private static bool IsStatus(HomeDashboardRow row, string status)
        {
            return Clean(row.Status) == Clean(status);
        }

        private static string Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private class HomeDashboardRow
        {
            public long AttrId { get; set; }

            public string? AttrNo { get; set; }

            public string? PropertyDescription { get; set; }

            public string? Sector { get; set; }

            public string? Status { get; set; }

            public string? TaskAssignedTo { get; set; }

            public string? TaskAssignedToUserId { get; set; }

            public string? Valuer { get; set; }

            public string? ValuerUserId { get; set; }

            public DateTime SubmissionDateTime { get; set; }

            public DateTime? UpdatedDate { get; set; }

            public DateTime? ValuerDecisionDateTime { get; set; }

            public bool IsWithdrawn { get; set; }
        }
    }
}
