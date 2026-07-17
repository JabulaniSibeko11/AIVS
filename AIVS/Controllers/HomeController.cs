using AIVS.Models;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AIVS.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IHomeDashboardService _homeDashboardService;

        public HomeController(
            IUserManagementService userManagementService,
            IHomeDashboardService homeDashboardService)
        {
            _userManagementService = userManagementService;
            _homeDashboardService = homeDashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? valuerUserId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser == null || currentUser.HasAccess == false)
            {
                ViewBag.AccessMessage = currentUser?.AccessMessage
                    ?? "You do not have access to AIVS.";

                return View("AccessDenied");
            }

            var model = await _homeDashboardService
                .BuildDashboardAsync(currentUser, valuerUserId);

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}