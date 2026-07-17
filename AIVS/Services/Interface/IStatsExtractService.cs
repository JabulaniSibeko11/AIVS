using AIVS.Models.ViewModels.Reports;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface IStatsExtractService
    {
        Task<byte[]> BuildExecutiveStatsExtractAsync(
            AivsCurrentUserVm currentUser,
            ExecutiveStatsExtractFilterVm filter);
    }
}
