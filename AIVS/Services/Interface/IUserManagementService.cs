using AIVS.Models.UserManagement;
using AIVS.Models.ViewModels.UserManagement;
using System.Security.Claims;

namespace AIVS.Services.Interface
{
    public interface IUserManagementService
    {
        Task<UserManagementResult?> ValidateAdminAsync(string sapNumber);

        Task<UserManagementResult?> ValidateByWindowsIdentityAsync(string windowsIdentityName);

        Task<AivsCurrentUserVm> GetCurrentUserAsync(ClaimsPrincipal user);
    }
}
