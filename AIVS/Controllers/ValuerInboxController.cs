using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers
{
    [Authorize]
    public class ValuerInboxController : Controller
    {
        private readonly IValuerInboxService _valuerInboxService;
        private readonly IUserManagementService _userManagementService;

        public ValuerInboxController(
            IValuerInboxService valuerInboxService,
            IUserManagementService userManagementService)
        {
            _valuerInboxService = valuerInboxService;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return View(new List<AIVS.Models.ViewModels.ValuerInbox.ValuerInboxItemVm>());
            }

            var model = await _valuerInboxService.GetMyInboxAsync(currentUser);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Review(long id)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _valuerInboxService.OpenReviewAsync(id, currentUser);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSectionReview(SaveSectionReviewVm vm, long attrId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _valuerInboxService.SaveSectionReviewAsync(vm, currentUser);

                TempData["Success"] = "Section review saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id = attrId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFinalReview(SubmitFinalReviewVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _valuerInboxService.SubmitFinalReviewAsync(vm, currentUser);

                TempData["Success"] = "Final review decision submitted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Review), new { id = vm.AttrId });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInspection(ScheduleInspectionVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _valuerInboxService.ScheduleInspectionAsync(vm, currentUser);

                TempData["Success"] = "Physical inspection request created successfully. The client can now select one of the three proposed dates.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id = vm.AttrId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInspectionDetails(long inspectionRequestId, long attrId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _valuerInboxService.SendInspectionDetailsToClientAsync(
                    inspectionRequestId,
                    currentUser);

                TempData["Success"] = "Inspection details and PIN were prepared successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id = attrId });
        }
    }
}