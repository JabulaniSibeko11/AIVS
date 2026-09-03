
namespace AIVS.Models.Configuration
{
    public class InspectionPinWorkerSettings
    {
        public bool Enabled { get; set; } = true;

        // Production interval.
        public int CheckEveryMinutes { get; set; } = 5;

        // UAT/demo override. When > 0 this takes priority over CheckEveryMinutes.
        public int CheckEverySeconds { get; set; } = 0;

        // Production/default behaviour.
        public int GenerateHoursBefore { get; set; } = 2;

        // Optional UAT minute override.
        public int GenerateMinutesBefore { get; set; } = 0;

        // DEMO ONLY:
        // When true, any future Confirmed appointment without a PIN is eligible
        // immediately, regardless of how far away the appointment is.
        public bool DemoGenerateImmediately { get; set; } = false;

        public int PinValidMinutesBefore { get; set; } = 30;
        public int PinValidMinutesAfter { get; set; } = 120;
    }
}
