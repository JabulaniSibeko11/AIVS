using AIVS.Models.ViewModels.Notifications;
using AIVS.Models.ViewModels.UserManagement;

namespace AIVS.Services.Interface
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            int? targetUserId,
            string? targetUsername,
            string? targetRole,
            string title,
            string message,
            string notificationType,
            long? attrId,
            string? attrNo,
            long? inspectionRequestId,
            string? createdBy);

        Task<int> GetUnreadCountAsync(AivsCurrentUserVm currentUser);

        Task<List<AivsNotificationVm>> GetMyNotificationsAsync(AivsCurrentUserVm currentUser);

        Task MarkAsReadAsync(long id, AivsCurrentUserVm currentUser);

        Task MarkAllAsReadAsync(AivsCurrentUserVm currentUser);
    }
}
