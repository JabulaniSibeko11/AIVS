using AIVS.Models.ViewModels.SectorManager;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIVS.Security;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.SectorManagerQa)]
    public class SectorManagerQaController : Controller
    {
        private readonly ISectorManagerQaService _sectorManagerQaService;
        private readonly IUserManagementService _userManagementService;
        private readonly IProcessorFileService _processorFiles;

        public SectorManagerQaController(
            ISectorManagerQaService sectorManagerQaService,
            IUserManagementService userManagementService,
            IProcessorFileService processorFiles)
        {
            _sectorManagerQaService = sectorManagerQaService;
            _userManagementService = userManagementService;
            _processorFiles = processorFiles;
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
        public async Task<IActionResult> UploadProcessorEvidence(long qaId, long attrId, List<IFormFile> files, string? evidenceComment)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            try
            {
                await _sectorManagerQaService.GetDetailsAsync(qaId, currentUser);
                await _processorFiles.UploadEvidenceAsync(attrId, files, evidenceComment, "Sector Manager QA", currentUser);
                TempData["Success"] = "Sector Manager supporting evidence uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id = qaId });
        }

        [HttpGet]
        public async Task<IActionResult> ProcessorEvidenceFile(long id, long evidenceId, bool download = false)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            try
            {
                await _sectorManagerQaService.GetDetailsAsync(id, currentUser);
                var file = await _processorFiles.GetEvidenceFileAsync(evidenceId);
                if (file == null) return NotFound("Processor evidence file was not found.");
                return download
                    ? PhysicalFile(file.Value.Path, file.Value.ContentType, file.Value.FileName, enableRangeProcessing: true)
                    : PhysicalFile(file.Value.Path, file.Value.ContentType, enableRangeProcessing: true);
            }
            catch { return Forbid(); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(long id)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            try
            {
                await _sectorManagerQaService.ClaimAsync(id, currentUser);
                TempData["Success"] = "The QA item was assigned to you.";
                return RedirectToAction(nameof(Details), new { id });
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
                TempData["Success"] = "Sector Manager QA approved. The submission was sent to Senior Manager QA.";
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
