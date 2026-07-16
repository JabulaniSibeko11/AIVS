namespace AIVS.Models.ViewModels.SectorInbox
{
    public class SectorValuerVm
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Role { get; set; }

        public string? Sector { get; set; }
    }
}
