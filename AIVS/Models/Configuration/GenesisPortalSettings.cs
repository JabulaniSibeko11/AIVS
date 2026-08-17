namespace AIVS.Models.Configuration
{
    public sealed class GenesisPortalSettings
    {
        public string PublicBaseUrl { get; set; } = string.Empty;
        public string InspectionAppointmentPath { get; set; } = "/attributes/inspection";
    }
}
