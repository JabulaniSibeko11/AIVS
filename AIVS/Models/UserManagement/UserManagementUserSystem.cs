namespace AIVS.Models.UserManagement
{
    public class UserManagementUserSystem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int SystemId { get; set; }

        public int RoleId { get; set; }

        public string? Sector { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
