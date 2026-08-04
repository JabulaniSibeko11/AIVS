using AIVS.Models.ViewModels.UserManagement;
using AIVS.Security;

namespace AIVS.Services.Interface;

public interface IAivsRoleAccessService
{
    string NormalizeRole(string? role);
    bool HasPermission(AivsCurrentUserVm? user, AivsPermission permission);
    bool IsValuer(AivsCurrentUserVm? user);
    bool IsSectorManager(AivsCurrentUserVm? user);
    bool IsSeniorManager(AivsCurrentUserVm? user);
    bool IsLeadership(AivsCurrentUserVm? user);
    bool IsAdministrator(AivsCurrentUserVm? user);
}
