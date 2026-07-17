using AIVS.Models.ViewModels.Home;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface IHomeDashboardService
    {
        Task<AivsHomeDashboardVm> BuildDashboardAsync(
            AivsCurrentUserVm currentUser,
            string? selectedValuerUserId);
    }
}
