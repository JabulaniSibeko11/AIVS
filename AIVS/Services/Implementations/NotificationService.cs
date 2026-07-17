using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.Notifications;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AttributesDbContext _context;

        public NotificationService(AttributesDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(
            int? targetUserId,
            string? targetUsername,
            string? targetRole,
            string title,
            string message,
            string notificationType,
            long? attrId,
            string? attrNo,
            long? inspectionRequestId,
            string? createdBy)
        {
            var notification = new AivsNotification
            {
                TargetUserId = targetUserId,
                TargetUsername = targetUsername,
                TargetRole = targetRole,
                Title = title,
                Message = message,
                NotificationType = notificationType,
                Attr_ID = attrId,
                Attr_No = attrNo,
                InspectionRequestId = inspectionRequestId,
                IsRead = false,
                CreatedBy = createdBy,
                CreatedDateTime = DateTime.Now
            };

            _context.AivsNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(AivsCurrentUserVm currentUser)
        {
            return await _context.AivsNotifications
                .AsNoTracking()
                .Where(x =>
                    x.IsRead == false &&
                    (
                        x.TargetUserId == currentUser.UserId ||
                        x.TargetUsername == currentUser.Username ||
                        x.TargetUsername == currentUser.WindowsUsername ||
                        x.TargetRole == currentUser.Role
                    ))
                .CountAsync();
        }

        public async Task<List<AivsNotificationVm>> GetMyNotificationsAsync(AivsCurrentUserVm currentUser)
        {
            return await _context.AivsNotifications
                .AsNoTracking()
                .Where(x =>
                    x.TargetUserId == currentUser.UserId ||
                    x.TargetUsername == currentUser.Username ||
                    x.TargetUsername == currentUser.WindowsUsername ||
                    x.TargetRole == currentUser.Role)
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x => x.CreatedDateTime)
                .Take(100)
                .Select(x => new AivsNotificationVm
                {
                    Id = x.Id,
                    Title = x.Title,
                    Message = x.Message,
                    NotificationType = x.NotificationType,
                    AttrId = x.Attr_ID,
                    AttrNo = x.Attr_No,
                    InspectionRequestId = x.InspectionRequestId,
                    IsRead = x.IsRead,
                    CreatedDateTime = x.CreatedDateTime
                })
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(long id, AivsCurrentUserVm currentUser)
        {
            var notification = await _context.AivsNotifications
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    (
                        x.TargetUserId == currentUser.UserId ||
                        x.TargetUsername == currentUser.Username ||
                        x.TargetUsername == currentUser.WindowsUsername ||
                        x.TargetRole == currentUser.Role
                    ));

            if (notification == null)
                return;

            notification.IsRead = true;
            notification.ReadDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(AivsCurrentUserVm currentUser)
        {
            var notifications = await _context.AivsNotifications
                .Where(x =>
                    x.IsRead == false &&
                    (
                        x.TargetUserId == currentUser.UserId ||
                        x.TargetUsername == currentUser.Username ||
                        x.TargetUsername == currentUser.WindowsUsername ||
                        x.TargetRole == currentUser.Role
                    ))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadDateTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }
    }
}