using AIVS.Models.ViewModels.InspectionCalendar;
using AIVS.Security;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.ReviewSubmission)]
    public class InspectionCalendarController : Controller
    {
        private readonly IInspectionCalendarService _calendarService;
        private readonly IUserManagementService _userManagementService;

        public InspectionCalendarController(IInspectionCalendarService calendarService, IUserManagementService userManagementService)
        {
            _calendarService = calendarService;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction("Index", "Home");
            }
            var selected = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            if (selected < currentMonth) selected = currentMonth;
            return View(await _calendarService.GetMonthAsync(currentUser, selected.Year, selected.Month));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDayAvailability(SetInspectionDayAvailabilityVm vm, int returnYear, int returnMonth)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            try { await _calendarService.SetDayAvailabilityAsync(currentUser, vm); TempData["Success"] = "Inspection calendar updated."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Index), new { year = returnYear, month = returnMonth });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBlock(long blockId, int returnYear, int returnMonth)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            try { await _calendarService.RemoveBlockAsync(currentUser, blockId); TempData["Success"] = "Calendar block removed."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToAction(nameof(Index), new { year = returnYear, month = returnMonth });
        }
    }
}
