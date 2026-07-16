namespace AIVS.Models.UserManagement
{
    public class UserManagementRole
    {
        public int Id { get; set; }

        public string? RoleName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
