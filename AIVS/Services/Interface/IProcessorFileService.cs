using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;
using Microsoft.AspNetCore.Http;

namespace AIVS.Services.Interface;

public interface IProcessorFileService
{
    Task<List<ProcessorEvidenceFileVm>> GetEvidenceAsync(long attrId);
    Task UploadEvidenceAsync(long attrId, IEnumerable<IFormFile> files, string? comment, string stage, AivsCurrentUserVm currentUser);
    Task<(string Path, string FileName, string ContentType)?> GetEvidenceFileAsync(long evidenceId);
    Task<string?> SaveClientEmailCopyAsync(
        string attrNo,
        string emailType,
        string fromEmail,
        string fromName,
        string originalToEmail,
        string actualToEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string htmlBody,
        bool isTestMode,
        byte[]? calendarInviteBytes = null,
        string? calendarInviteFileName = null,
        byte[]? attachmentBytes = null,
        string? attachmentFileName = null,
        string? attachmentContentType = null);
}
