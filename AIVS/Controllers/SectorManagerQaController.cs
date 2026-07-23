using AIVS.Models.ViewModels.SectorManager;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers
{
    [Authorize]
    public class SectorManagerQaController : Controller
    {
        private readonly ISectorManagerQaService _sectorManagerQaService;
        private readonly IUserManagementService _userManagementService;

        public SectorManagerQaController(
            ISectorManagerQaService sectorManagerQaService,
            IUserManagementService userManagementService)
        {
            _sectorManagerQaService = sectorManagerQaService;
            _userManagementService = userManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
            {
                TempData["Error"] = "Your AIVS user could not be verified.";
                return View(new List<SectorManagerQaInboxItemVm>());
            }

            var model = await _sectorManagerQaService.GetInboxAsync(currentUser);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
            {
                TempData["Error"] = "Your AIVS user could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var model = await _sectorManagerQaService.GetDetailsAsync(id, currentUser);
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
        public async Task<IActionResult> Approve(SectorManagerQaDecisionVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
            {
                TempData["Error"] = "Your AIVS user could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _sectorManagerQaService.ApproveToOvvioAsync(vm, currentUser);
                TempData["Success"] = "Sector Manager QA approved. The submission is now ready for OVVIO extract.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = vm.QaId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToValuer(SectorManagerQaDecisionVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
            {
                TempData["Error"] = "Your AIVS user could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _sectorManagerQaService.ReturnToValuerAsync(vm, currentUser);
                TempData["Success"] = "The submission was returned to the valuer for rework.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = vm.QaId });
            }
        }
    }
}
