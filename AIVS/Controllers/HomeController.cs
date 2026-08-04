using AIVS.Models;
using AIVS.Models.ViewModels.Reports;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using AIVS.Security;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.AccessAivs)]
    public class HomeController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IHomeDashboardService _homeDashboardService;
        private readonly IStatsExtractService _statsExtractService;
        public HomeController(
            IUserManagementService userManagementService,
            IHomeDashboardService homeDashboardService,
            IStatsExtractService statsExtractService)
        {
            _userManagementService = userManagementService;
            _homeDashboardService = homeDashboardService;
            _statsExtractService = statsExtractService;
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
        [HttpGet]
        [Authorize(Policy = AivsPolicyNames.ExportStatistics)]
        public async Task<IActionResult> ExportExecutiveStats(
    string periodType = "Monthly",
    DateTime? fromDate = null,
    DateTime? toDate = null)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser == null || currentUser.HasAccess == false)
                return Forbid();

            var filter = new ExecutiveStatsExtractFilterVm
            {
                PeriodType = periodType,
                FromDate = fromDate,
                ToDate = toDate
            };

            var bytes = await _statsExtractService
                .BuildExecutiveStatsExtractAsync(currentUser, filter);

            var resolvedPeriod = string.IsNullOrWhiteSpace(periodType)
                ? "Monthly"
                : periodType.Trim();

            var fileName =
                $"AIVS_Executive_Stats_{resolvedPeriod}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Help()
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
