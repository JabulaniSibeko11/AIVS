
namespace AIVS.Models.Configuration
{
    public class InspectionPinWorkerSettings
    {
        public bool Enabled { get; set; } = true;

        // Worker wakes up on this interval.
        public int CheckEveryMinutes { get; set; } = 5;

        // Generate the PIN this many hours before the confirmed appointment.
        public int GenerateHoursBefore { get; set; } = 2;

        // PIN becomes usable shortly before arrival.
        public int PinValidMinutesBefore { get; set; } = 30;

        // PIN remains usable after appointment start for this many minutes.
        public int PinValidMinutesAfter { get; set; } = 120;
    }
}
