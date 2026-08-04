using AIVS.Models.ViewModels.SectorManager;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.SeniorManager;

namespace AIVS.Services.Interface
{
    public interface ISectorManagerQaService
    {
        Task<List<SectorManagerQaInboxItemVm>> GetInboxAsync(AivsCurrentUserVm currentUser);

        Task<SectorManagerQaDetailsVm> GetDetailsAsync(long qaId, AivsCurrentUserVm currentUser);

        Task ClaimAsync(long qaId, AivsCurrentUserVm currentUser);

        Task ApproveToOvvioAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);

        Task ReturnToValuerAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);

        Task<List<SeniorManagerQaInboxItemVm>> GetSeniorManagerInboxAsync(AivsCurrentUserVm currentUser);

        Task ClaimSeniorManagerQaAsync(long qaId, AivsCurrentUserVm currentUser);

        Task<SectorManagerQaDetailsVm> GetSeniorManagerDetailsAsync(long qaId, AivsCurrentUserVm currentUser);

        Task ApproveSeniorManagerQaAsync(SeniorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);

        Task ReturnToSectorManagerAsync(SeniorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser);
    }
}
