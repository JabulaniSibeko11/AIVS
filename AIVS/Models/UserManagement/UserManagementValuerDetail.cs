namespace AIVS.Models.UserManagement
{
    public class UserManagementValuerDetail
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string? Pin { get; set; }

        public string? VehicleRegistration { get; set; }

        public string? VehicleMake { get; set; }

        public string? VehicleColour { get; set; }

        public string? CellNumber { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
