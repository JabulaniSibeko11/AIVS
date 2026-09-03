
namespace AIVS.Models.Configuration
{
    public class InspectionPinWorkerSettings
    {
        public bool Enabled { get; set; } = true;

        // Worker wakes up on this interval.
        public int CheckEveryMinutes { get; set; } = 5;

        // Production/default behaviour:
        // generate the PIN this many hours before the appointment.
        public int GenerateHoursBefore { get; set; } = 2;

        // UAT/testing override.
        // When > 0, this takes priority over GenerateHoursBefore.
        // Keep at 0 in production to use the hour-based setting.
        public int GenerateMinutesBefore { get; set; } = 0;

        // PIN becomes usable shortly before arrival.
        public int PinValidMinutesBefore { get; set; } = 30;

        // PIN remains usable after appointment start for this many minutes.
        public int PinValidMinutesAfter { get; set; } = 120;
    }
}
