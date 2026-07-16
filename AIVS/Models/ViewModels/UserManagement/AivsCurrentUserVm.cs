namespace AIVS.Models.ViewModels.UserManagement
{
    public class AivsCurrentUserVm
    {
        public bool HasAccess { get; set; }

        public string WindowsUsername { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public string? Username { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        // This is the UserManagement role.
        // Example: SECTOR MANAGER, VALUER, ADMIN
        public string? Role { get; set; }

        // This is only for display.
        // Example: Professional Valuer, Data Manager, Admin Officer
        public string? Position { get; set; }

        public string? Sector { get; set; }

        public string? Pin { get; set; }

        public string? VehicleRegistration { get; set; }

        public string? VehicleMake { get; set; }

        public string? VehicleColour { get; set; }

        public string? CellNumber { get; set; }

        public string? AccessMessage { get; set; }
    }
}
