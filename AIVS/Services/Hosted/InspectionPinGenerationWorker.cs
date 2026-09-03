using AIVS.Data;
using AIVS.Models.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;

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
                _logger.LogInformation("Inspection PIN worker is disabled.");
                return;
            }

            var interval = ResolveCheckInterval();

            _logger.LogInformation(
                "Inspection PIN worker started. Interval={Interval}. DemoImmediate={DemoImmediate}. LeadTime={LeadTime}.",
                interval,
                _settings.DemoGenerateImmediately,
                ResolveGenerationLeadTime());

            // Run once immediately at application start before waiting.
            await RunSafeCycleAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await RunSafeCycleAsync(stoppingToken);
            }
        }

        private async Task RunSafeCycleAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                await GenerateDuePinsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Inspection PIN generation worker failed during this cycle.");
            }
        }

        private async Task GenerateDuePinsAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AttributesDbContext>();

            var now = DateTime.Now;
            var leadTime = ResolveGenerationLeadTime();
            var dueUntil = now.Add(leadTime);

            var query = db.AttrInspectionRequests
                .Where(x =>
                    x.Status == "Confirmed" &&
                    x.ConfirmedDateTime != null &&
                    x.ConfirmedDateTime > now &&
                    (x.InspectionPin == null || x.InspectionPin == ""));

            if (!_settings.DemoGenerateImmediately)
            {
                query = query.Where(x =>
                    x.ConfirmedDateTime <= dueUntil);
            }

            var requests = await query
                .OrderBy(x => x.ConfirmedDateTime)
                .Take(100)
                .ToListAsync(cancellationToken);

            if (requests.Count == 0)
                return;

            foreach (var request in requests)
            {
                var appointment = request.ConfirmedDateTime!.Value;

                request.InspectionPin = GeneratePin();
                request.InspectionPinGeneratedAt = now;

                // In demo mode make the PIN usable immediately so the full
                // Generate -> Send Details -> Client Verify flow can be shown.
                request.PinValidFrom =
                    _settings.DemoGenerateImmediately
                        ? now
                        : appointment.AddMinutes(
                            -Math.Max(0, _settings.PinValidMinutesBefore));

                request.PinValidUntil =
                    appointment.AddMinutes(
                        Math.Max(1, _settings.PinValidMinutesAfter));

                // If a demo appointment was selected far in the future, ensure
                // the PIN does not expire before the presentation can verify it.
                if (_settings.DemoGenerateImmediately &&
                    request.PinValidUntil <= now)
                {
                    request.PinValidUntil =
                        now.AddMinutes(
                            Math.Max(30, _settings.PinValidMinutesAfter));
                }

                request.UpdatedBy = "InspectionPinGenerationWorker";
                request.UpdatedDate = now;
            }

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated {Count} inspection PIN(s). DemoImmediate={DemoImmediate}.",
                requests.Count,
                _settings.DemoGenerateImmediately);
        }

        private TimeSpan ResolveCheckInterval()
        {
            if (_settings.CheckEverySeconds > 0)
            {
                return TimeSpan.FromSeconds(
                    Math.Max(1, _settings.CheckEverySeconds));
            }

            return TimeSpan.FromMinutes(
                Math.Max(1, _settings.CheckEveryMinutes));
        }

        private TimeSpan ResolveGenerationLeadTime()
        {
            if (_settings.GenerateMinutesBefore > 0)
            {
                return TimeSpan.FromMinutes(
                    Math.Max(1, _settings.GenerateMinutesBefore));
            }

            return TimeSpan.FromHours(
                Math.Max(1, _settings.GenerateHoursBefore));
        }

        private static string GeneratePin()
        {
            return RandomNumberGenerator
                .GetInt32(1000, 10000)
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}
