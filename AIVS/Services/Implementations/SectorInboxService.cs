using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.SectorInbox;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Implementations
{
    public class SectorInboxService : ISectorInboxService
    {
        private readonly AttributesDbContext _context;
        private readonly ILogger<SectorInboxService> _logger;

        public SectorInboxService(
            AttributesDbContext context,
            ILogger<SectorInboxService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SectorInboxPageVm> BuildSectorInboxAsync(string? selectedSector)
        {
            var tiles = await GetSectorTilesAsync();

            var model = new SectorInboxPageVm
            {
                SelectedSector = selectedSector,
                Tiles = tiles
            };

            if (!string.IsNullOrWhiteSpace(selectedSector))
            {
                model.Items = await GetSectorItemsAsync(selectedSector);
            }

            return model;
        }

        public async Task<List<SectorInboxTileVm>> GetSectorTilesAsync()
        {
            var sectors = await _context.Sectors
                .AsNoTracking()
                .Where(x => x.SECTOR != null && x.SECTOR.Trim() != "")
                .Select(x => x.SECTOR!.Trim())
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var counts = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Where(x =>
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == "") &&
                    x.RoutedSector != null)
                .GroupBy(x => x.RoutedSector!.Trim())
                .Select(g => new
                {
                    Sector = g.Key,
                    Total = g.Count()
                })
                .ToListAsync();

            var result = sectors
                .Select(sector => new SectorInboxTileVm
                {
                    Sector = sector,
                    Total = counts.FirstOrDefault(c =>
                        c.Sector.Equals(sector, StringComparison.OrdinalIgnoreCase))?.Total ?? 0
                })
                .ToList();

            return result;
        }

        public async Task<List<SectorInboxItemVm>> GetSectorItemsAsync(string sector)
        {
            if (string.IsNullOrWhiteSpace(sector))
                return new List<SectorInboxItemVm>();

            var cleanedSector = sector.Trim().ToUpper();

            return await _context.AttrPropertyInfo
                .AsNoTracking()
                .Include(x => x.PropertyDetails)
                .Where(x =>
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == "") &&
                    x.RoutedSector != null &&
                    x.RoutedSector.Trim().ToUpper() == cleanedSector)
                .OrderBy(x => x.RoutedToSectorDateTime)
                .Select(x => new SectorInboxItemVm
                {
                    AttrId = x.Attr_ID,
                    AttrNo = x.Attr_No,
                    PropertyDescription = x.Property_Desc,
                    Township = x.PropertyDetails != null ? x.PropertyDetails.Township : null,
                    RoutedSector = x.RoutedSector,
                    SubmittedDate = x.SubmissionDateTime,
                    RoutedDate = x.RoutedToSectorDateTime,
                    EvidenceCount = x.Evidence_Count,
                    Status = x.Attr_Status
                })
                .ToListAsync();
        }
        public async Task<SectorAssignmentResultVm> AssignSelectedToMeAsync(
        List<long> attrIds,
        int userId,
        string username,
        string fullName,
        string? email,
        string? role)
        {
            if (attrIds == null || attrIds.Count == 0)
                throw new InvalidOperationException("Please select at least one attribute submission.");

            attrIds = attrIds.Distinct().ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var items = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Where(x =>
                    attrIds.Contains(x.Attr_ID) &&
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == ""))
                .ToListAsync();

            if (items.Count != attrIds.Count)
                throw new InvalidOperationException("Some selected items are no longer in Sector Inbox. Refresh the page and try again.");

            var hasActiveAssignment = await _context.AttrValuerAssignments
                .AnyAsync(x => attrIds.Contains(x.Attr_ID) && x.AssignmentStatus == "Active");

            if (hasActiveAssignment)
                throw new InvalidOperationException("Some selected items already have an active assignment. Refresh the page and try again.");

            var now = DateTime.Now;

            var claimedCount = await _context.AttrPropertyInfo
                .Where(x =>
                    attrIds.Contains(x.Attr_ID) &&
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == ""))
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Task_Assigned_To_UserId, userId.ToString())
                    .SetProperty(x => x.Task_Assigned_To, fullName)
                    .SetProperty(x => x.Task_Assigned_DateTime, now)
                    .SetProperty(x => x.Task_Assigner, fullName)
                    .SetProperty(x => x.TaskAssignerComment, "User claimed selected items from the shared Sector Inbox.")
                    .SetProperty(x => x.Attr_Status, "Claimed")
                    .SetProperty(x => x.UpdatedBy, username)
                    .SetProperty(x => x.UpdatedDate, now));

            if (claimedCount != attrIds.Count)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Another user claimed one or more selected items. Refresh the page and try again.");
            }

            var result = new SectorAssignmentResultVm
            {
                Sector = items.FirstOrDefault()?.RoutedSector
            };

            foreach (var item in items)
            {
                var oldStatus = item.Attr_Status;

                _context.AttrValuerAssignments.Add(new AttrValuerAssignment
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,

                    AssignedToUserId = userId,
                    AssignedToUsername = username,
                    AssignedToName = fullName,
                    AssignedToEmail = email,
                    AssignedToRole = role,

                    AssignedSector = item.RoutedSector,

                    AssignmentType = "SelfAssigned",
                    AssignmentStatus = "Active",

                    AssignedByUserId = userId,
                    AssignedByUsername = username,
                    AssignedByName = fullName,

                    AssignedAt = now,

                    CreatedBy = username,
                    CreatedDate = now
                });

                _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    Action = "Self Assigned",
                    OldStatus = oldStatus,
                    NewStatus = "Claimed",
                    ActionByUserId = username,
                    ActionByName = fullName,
                    ActionRole = role ?? "Valuer",
                    Comment = "Valuer assigned the attribute submission to themselves from Sector Inbox.",
                    ActionDateTime = now
                });

                if (!string.IsNullOrWhiteSpace(item.Attr_No))
                    result.AssignedReferences.Add(item.Attr_No);
            }

            result.AssignedCount = items.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }
        public async Task<SectorAssignmentResultVm> AssignSelectedToValuerAsync(
    List<long> attrIds,
    int valuerUserId,
    string valuerUsername,
    string valuerFullName,
    string? valuerEmail,
    string? valuerRole,
    int managerUserId,
    string managerUsername,
    string managerFullName,
    string? managerRole)
        {
            if (attrIds == null || attrIds.Count == 0)
                throw new InvalidOperationException("Please select at least one attribute submission.");

            attrIds = attrIds.Distinct().ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var items = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Where(x =>
                    attrIds.Contains(x.Attr_ID) &&
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == ""))
                .ToListAsync();

            if (items.Count != attrIds.Count)
                throw new InvalidOperationException("Some selected items are no longer in Sector Inbox. Refresh the page and try again.");

            var hasActiveAssignment = await _context.AttrValuerAssignments
                .AnyAsync(x => attrIds.Contains(x.Attr_ID) && x.AssignmentStatus == "Active");

            if (hasActiveAssignment)
                throw new InvalidOperationException("Some selected items already have an active assignment. Refresh the page and try again.");

            var now = DateTime.Now;

            var assignedCount = await _context.AttrPropertyInfo
                .Where(x =>
                    attrIds.Contains(x.Attr_ID) &&
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Attr_Status == "SectorInbox" &&
                    (x.Task_Assigned_To_UserId == null || x.Task_Assigned_To_UserId == ""))
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Task_Assigned_To_UserId, valuerUserId.ToString())
                    .SetProperty(x => x.Task_Assigned_To, valuerFullName)
                    .SetProperty(x => x.Task_Assigned_DateTime, now)
                    .SetProperty(x => x.Task_Assigner, managerFullName)
                    .SetProperty(x => x.TaskAssignerComment, $"Assigned to {valuerFullName} from the shared Sector Inbox.")
                    .SetProperty(x => x.Attr_Status, "Claimed")
                    .SetProperty(x => x.UpdatedBy, managerUsername)
                    .SetProperty(x => x.UpdatedDate, now));

            if (assignedCount != attrIds.Count)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Another user claimed one or more selected items. Refresh the page and try again.");
            }

            var result = new SectorAssignmentResultVm
            {
                Sector = items.FirstOrDefault()?.RoutedSector
            };

            foreach (var item in items)
            {
                var oldStatus = item.Attr_Status;

                _context.AttrValuerAssignments.Add(new AttrValuerAssignment
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,

                    AssignedToUserId = valuerUserId,
                    AssignedToUsername = valuerUsername,
                    AssignedToName = valuerFullName,
                    AssignedToEmail = valuerEmail,
                    AssignedToRole = valuerRole,

                    AssignedSector = item.RoutedSector,

                    AssignmentType = "ManagerAssigned",
                    AssignmentStatus = "Active",

                    AssignedByUserId = managerUserId,
                    AssignedByUsername = managerUsername,
                    AssignedByName = managerFullName,

                    AssignedAt = now,

                    CreatedBy = managerUsername,
                    CreatedDate = now
                });

                _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    Action = "Manager Assigned",
                    OldStatus = oldStatus,
                    NewStatus = "Claimed",
                    ActionByUserId = managerUsername,
                    ActionByName = managerFullName,
                    ActionRole = managerRole ?? "Sector Manager",
                    Comment = $"Sector Manager assigned the attribute submission to {valuerFullName}.",
                    ActionDateTime = now
                });

                if (!string.IsNullOrWhiteSpace(item.Attr_No))
                    result.AssignedReferences.Add(item.Attr_No);
            }

            result.AssignedCount = items.Count;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }

    }
}
