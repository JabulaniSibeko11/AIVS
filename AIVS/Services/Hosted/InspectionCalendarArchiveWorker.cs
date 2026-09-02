
using AIVS.Data;
using AIVS.Models.Attributes;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Hosted
{
    public class InspectionCalendarArchiveWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InspectionCalendarArchiveWorker> _logger;

        public InspectionCalendarArchiveWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<InspectionCalendarArchiveWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ArchivePreviousMonthsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Inspection calendar archive worker failed during this cycle.");
                }

                // Daily is enough. The operation is idempotent.
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task ArchivePreviousMonthsAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AttributesDbContext>();

            var currentMonthStart =
                new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var alreadyArchivedIds =
                db.AttrInspectionCalendarBlockArchives
                    .AsNoTracking()
                    .Select(x => x.OriginalBlockId);

            var oldBlocks = await db.AttrInspectionCalendarBlocks
                .AsNoTracking()
                .Where(x =>
                    x.BlockedTo < currentMonthStart &&
                    !alreadyArchivedIds.Contains(x.Id))
                .OrderBy(x => x.BlockedFrom)
                .Take(500)
                .ToListAsync(cancellationToken);

            if (oldBlocks.Count == 0)
                return;

            var now = DateTime.Now;
            var archiveBatchMonth = currentMonthStart.Date;

            foreach (var block in oldBlocks)
            {
                db.AttrInspectionCalendarBlockArchives.Add(
                    new AttrInspectionCalendarBlockArchive
                    {
                        OriginalBlockId = block.Id,
                        UserId = block.UserId,
                        BlockedFrom = block.BlockedFrom,
                        BlockedTo = block.BlockedTo,
                        IsWholeDay = block.IsWholeDay,
                        Reason = block.Reason,
                        SourceIsActive = block.IsActive,
                        OriginalCreatedBy = block.CreatedBy,
                        OriginalCreatedDate = block.CreatedDate,
                        OriginalUpdatedBy = block.UpdatedBy,
                        OriginalUpdatedDate = block.UpdatedDate,
                        ArchivedDate = now,
                        ArchiveBatchMonth = archiveBatchMonth
                    });
            }

            // IMPORTANT: nothing is deleted from AttrInspectionCalendarBlock.
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Archived {Count} historical calendar block(s) without deleting source rows.",
                oldBlocks.Count);
        }
    }
}
