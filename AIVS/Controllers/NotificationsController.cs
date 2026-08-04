using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIVS.Security;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.AccessAivs)]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly IUserManagementService _userManagementService;

        public NotificationsController(
            INotificationService notificationService,
            IUserManagementService userManagementService)
        {
            _notificationService = notificationService;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();

            var notifications = await _notificationService
                .GetMyNotificationsAsync(currentUser);

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var currentUser = await GetCurrentUserAsync();

            await _notificationService.MarkAsReadAsync(id, currentUser);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUser = await GetCurrentUserAsync();

            await _notificationService.MarkAllAsReadAsync(currentUser);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var currentUser = await GetCurrentUserAsync();

            var count = await _notificationService
                .GetUnreadCountAsync(currentUser);

            return Json(new { count });
        }

        private async Task<AivsCurrentUserVm> GetCurrentUserAsync()
        {
            if (User?.Identity == null || !User.Identity.IsAuthenticated)
                throw new InvalidOperationException("Current Windows user could not be resolved.");

            return await _userManagementService.GetCurrentUserAsync(User);
        }
    }
}
