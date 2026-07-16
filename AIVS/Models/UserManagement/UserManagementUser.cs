namespace AIVS.Models.UserManagement
{
    public class UserManagementUser
    {
        public int Id { get; set; }

        public string? Username { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
