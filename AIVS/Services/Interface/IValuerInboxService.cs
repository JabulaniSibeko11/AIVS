using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;

namespace AIVS.Services.Interface
{
    public interface IValuerInboxService
    {
        Task<List<ValuerInboxItemVm>> GetMyInboxAsync(AivsCurrentUserVm currentUser);

        Task<ValuerReviewPageVm> OpenReviewAsync(long attrId, AivsCurrentUserVm currentUser);
        Task SaveSectionReviewAsync(
    SaveSectionReviewVm vm,
    AivsCurrentUserVm currentUser);
        Task SubmitFinalReviewAsync(
    SubmitFinalReviewVm vm,
    AivsCurrentUserVm currentUser);

        Task ScheduleInspectionAsync(
    ScheduleInspectionVm vm,
    AivsCurrentUserVm currentUser);

        Task SendInspectionDetailsToClientAsync(
    long inspectionRequestId,
    AivsCurrentUserVm currentUser);
    }
}
