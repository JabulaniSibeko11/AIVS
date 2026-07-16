namespace AIVS.Services.Interface
{
    public interface IEmailService
    {
        Task SendSelfAssignmentEmailAsync(
            string toEmail,
            string fullName,
            string sector,
            int assignedCount,
            List<string> references);

        Task SendManagerAssignmentEmailAsync(
    string toEmail,
    string valuerFullName,
    string managerFullName,
    string sector,
    int assignedCount,
    List<string> references);
    }
}
