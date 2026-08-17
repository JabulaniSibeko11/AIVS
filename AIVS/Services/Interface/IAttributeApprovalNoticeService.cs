using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface;

public interface IAttributeApprovalNoticeService
{
    Task<string> GenerateAsync(AttrPropertyInfo item, string approvalComment, AivsCurrentUserVm approver);
}
