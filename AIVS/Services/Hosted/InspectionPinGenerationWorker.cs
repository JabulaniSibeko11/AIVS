using AIVS.Data;
using AIVS.Models.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Globalization;

namespace AIVS.Services.Hosted
{
    public class InspectionPinGenerationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InspectionPinGenerationWorker> _logger;
        private readonly InspectionPinWorkerSettings _settings;

        public InspectionPinGenerationWorker(
            IServiceScopeFactory scopeFactory,
            IOptions<InspectionPinWorkerSettings> settings,
            ILogger<InspectionPinGenerationWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation(
                    "Inspection PIN worker is disabled.");
                return;
            }

            var interval = TimeSpan.FromMinutes(
                Math.Max(1, _settings.CheckEveryMinutes));

            var configuredLeadTime =
                ResolveGenerationLeadTime();

            _logger.LogInformation(
                "Inspection PIN worker started. CheckEvery={CheckEveryMinutes} minute(s), GenerateBefore={GenerateBefore}.",
                Math.Max(1, _settings.CheckEveryMinutes),
                configuredLeadTime);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateDuePinsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Inspection PIN generation worker failed during this cycle.");
                }

                try
                {
                    await Task.Delay(
                        interval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task GenerateDuePinsAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AttributesDbContext>();

            var now = DateTime.Now;

            var leadTime =
                ResolveGenerationLeadTime();

            var dueUntil =
                now.Add(leadTime);

            var requests = await db.AttrInspectionRequests
                .Where(x =>
                    x.Status == "Confirmed" &&
                    x.ConfirmedDateTime != null &&
                    x.ConfirmedDateTime > now &&
                    x.ConfirmedDateTime <= dueUntil &&
                    (x.InspectionPin == null ||
                     x.InspectionPin == ""))
                .OrderBy(x => x.ConfirmedDateTime)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (requests.Count == 0)
                return;

            foreach (var request in requests)
            {
                var appointment =
                    request.ConfirmedDateTime!.Value;

                request.InspectionPin =
                    GeneratePin();

                request.InspectionPinGeneratedAt =
                    now;

                request.PinValidFrom =
                    appointment.AddMinutes(
                        -Math.Max(
                            0,
                            _settings.PinValidMinutesBefore));

                request.PinValidUntil =
                    appointment.AddMinutes(
                        Math.Max(
                            1,
                            _settings.PinValidMinutesAfter));

                request.UpdatedBy =
                    "InspectionPinGenerationWorker";

                request.UpdatedDate =
                    now;
            }

            await db.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Generated {Count} inspection PIN(s). LeadTime={LeadTime}.",
                requests.Count,
                leadTime);
        }

        private TimeSpan ResolveGenerationLeadTime()
        {
            // UAT/testing: minutes take priority when configured.
            if (_settings.GenerateMinutesBefore > 0)
            {
                return TimeSpan.FromMinutes(
                    Math.Max(
                        1,
                        _settings.GenerateMinutesBefore));
            }

            // Production/default.
            return TimeSpan.FromHours(
                Math.Max(
                    1,
                    _settings.GenerateHoursBefore));
        }

        private static string GeneratePin()
        {
            // Preserve the existing 4-digit business workflow,
            // but use a cryptographically secure generator.
            return RandomNumberGenerator
                .GetInt32(1000, 10000)
                .ToString(
                    CultureInfo.InvariantCulture);
        }
    }
}
