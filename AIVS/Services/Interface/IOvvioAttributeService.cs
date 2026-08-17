using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface;

public interface IOvvioAttributeService
{
    Task<AttrOvvioApprovedAttribute> InsertApprovedSubmissionAsync(
        AttrPropertyInfo item,
        string approvalComment,
        string? approvalNoticePath,
        AivsCurrentUserVm currentUser);
}
