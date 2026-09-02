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

        //Task SendInspectionDateOptionsEmailAsync(
        //    string toEmail,
        //    string? clientName,
        //    string attrNo,
        //    string? propertyDescription,
        //    List<DateTime> proposedDates,
        //    string? requestComment,
        //    string? secureGenesisLink = null);

        Task SendInspectionCalendarEmailAsync(
    string toEmail,
    string? clientName,
    string attrNo,
    string? propertyDescription,
    string? requestComment,
    string? secureGenesisLink = null);


        Task SendInspectionDetailsEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            DateTime confirmedDateTime,
            string inspectionPin,
            string valuerName,
            string? valuerEmail,
            string? valuerCell,
            string? vehicleRegistration,
            string? vehicleMake,
            string? vehicleColour,
            string? photoFileName,
            string? secureGenesisLink = null);

        Task SendReturnedToClientEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment);

        Task SendRejectedEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment);

        Task SendAcceptedEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment);

        Task SendAttributeApprovalEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment,
            byte[] approvalNoticeBytes,
            string approvalNoticeFileName);
    }
}