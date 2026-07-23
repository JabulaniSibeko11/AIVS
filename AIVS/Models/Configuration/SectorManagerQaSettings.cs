namespace AIVS.Models.Configuration
{
    public class SectorManagerQaSettings
    {
        public bool Enabled { get; set; } = true;

        public int WeeklySamplePercent { get; set; } = 10;

        public int MinimumWeeklySamplePerSector { get; set; } = 5;
    }
}
