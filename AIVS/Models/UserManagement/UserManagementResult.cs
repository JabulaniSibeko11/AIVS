namespace AIVS.Models.UserManagement
{
    public class UserManagementResult
    {
        public int UserID { get; set; }

        public string Username { get; set; } = string.Empty;

        public bool Active { get; set; }

        public string? FirstName { get; set; }

        public string? SecondName { get; set; }

        public string? Surname { get; set; }

        public string? Position { get; set; }

        public string? SAPNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public string? EmailAddress { get; set; }

        public string FullName =>
            string.Join(" ",
                new[] { FirstName?.Trim(), Surname?.Trim() }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

        public string DisplayLabel =>
            string.IsNullOrWhiteSpace(Position)
                ? FullName
                : $"{FullName} — {Position.Trim()}";
    }
}
