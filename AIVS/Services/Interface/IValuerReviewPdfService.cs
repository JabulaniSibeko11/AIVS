using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface IValuerReviewPdfService
    {
        Task<string> GenerateReviewedFormPdfAsync(
            long reviewId,
            AivsCurrentUserVm currentUser);
    }
}
