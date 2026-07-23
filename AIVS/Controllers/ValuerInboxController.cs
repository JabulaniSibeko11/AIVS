using AIVS.Data;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Controllers
{
    [Authorize]
    public class ValuerInboxController : Controller
    {
        private readonly IValuerInboxService _valuerInboxService;
        private readonly IUserManagementService _userManagementService;
        private readonly AttributesDbContext _context;
        public ValuerInboxController(
            IValuerInboxService valuerInboxService,
            IUserManagementService userManagementService,
            AttributesDbContext context)
            
        {
            _valuerInboxService = valuerInboxService;
            _userManagementService = userManagementService;
            _context = context;
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
        [HttpGet]
        public async Task<IActionResult> PhysicalInspectionEvidenceFile(long id)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
                return Forbid();

            var evidence = await _context.AttrInspectionEvidence
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive == true);

            if (evidence == null)
                return NotFound("Physical inspection evidence could not be found.");

            var item = await _context.AttrPropertyInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == evidence.Attr_ID &&
                    x.IsActive == true);

            if (item == null)
                return NotFound("Attribute submission could not be found.");

            var userIdText = currentUser.UserId.Value.ToString();

            if (item.Task_Assigned_To_UserId != userIdText)
                return Forbid();

            if (string.IsNullOrWhiteSpace(evidence.FilePath))
                return NotFound("Evidence file path is missing.");

            if (!System.IO.File.Exists(evidence.FilePath))
                return NotFound("Evidence file does not exist on the server.");

            var contentType = !string.IsNullOrWhiteSpace(evidence.ContentType)
                ? evidence.ContentType
                : GetContentType(evidence.FileName);

            var fileName = string.IsNullOrWhiteSpace(evidence.FileName)
                ? Path.GetFileName(evidence.FilePath)
                : evidence.FileName;

            var bytes = await System.IO.File.ReadAllBytesAsync(evidence.FilePath);

            return File(bytes, contentType, fileName);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        [HttpGet]
        public async Task<IActionResult> ReviewedPdf(long attrId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (currentUser.UserId == null)
                return Forbid();

            var userIdText = currentUser.UserId.Value.ToString();

            var item = await _context.AttrPropertyInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == attrId &&
                    x.IsActive == true);

            if (item == null)
                return NotFound("Attribute submission could not be found.");

            if (item.Task_Assigned_To_UserId != userIdText)
                return Forbid();

            if (string.IsNullOrWhiteSpace(item.ValuerEvidencePath))
                return NotFound("Reviewed PDF has not been generated yet.");

            if (!System.IO.File.Exists(item.ValuerEvidencePath))
                return NotFound("Reviewed PDF file does not exist on the server.");

            var fileName = Path.GetFileName(item.ValuerEvidencePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(item.ValuerEvidencePath);

            return File(bytes, "application/pdf", fileName);
        }
    }
}