using AIVS.Models.ViewModels.SectorManager;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface ISectorManagerQaService
    {
        Task<List<SectorManagerQaInboxItemVm>> GetInboxAsync(AivsCurrentUserVm currentUser);

        Task<SectorManagerQaDetailsVm> GetDetailsAsync(long qaId, AivsCurrentUserVm currentUser);

        Task ApproveToOvvioAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);

        Task ReturnToValuerAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);
    }
}
