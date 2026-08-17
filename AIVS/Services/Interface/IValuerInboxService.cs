using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;

namespace AIVS.Services.Interface
{
    public interface IValuerInboxService
    {
        Task<List<ValuerInboxItemVm>> GetMyInboxAsync(AivsCurrentUserVm currentUser);

        Task<ValuerReviewPageVm> OpenReviewAsync(long attrId, AivsCurrentUserVm currentUser);
        Task<ValuerReviewPageVm> GetReviewForQaAsync(long reviewId, AivsCurrentUserVm currentUser);
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

        Task CompleteInspectionAsync(
    long inspectionRequestId,
    AivsCurrentUserVm currentUser);
        Task SaveDraftAsync(SaveReviewDraftVm vm, AivsCurrentUserVm currentUser);
        Task SaveCorrectionFieldsAsync(SaveCorrectionFieldsVm vm, AivsCurrentUserVm currentUser);
        Task ResolveRatingDifferenceAsync(ResolveRatingDifferenceVm vm, AivsCurrentUserVm currentUser);
        Task SaveQuickSectionDecisionAsync(QuickSectionDecisionVm vm, AivsCurrentUserVm currentUser);
    }
}
