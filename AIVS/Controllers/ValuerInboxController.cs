using AIVS.Data;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using AIVS.Security;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.ReviewSubmission)]
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
        public async Task<IActionResult> AutoSaveDraft([FromBody] SaveReviewDraftVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess) return Forbid();
            try
            {
                await _valuerInboxService.SaveDraftAsync(vm, currentUser);
                return Json(new { success = true, savedAt = DateTime.Now.ToString("HH:mm:ss") });
            }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCorrectionFields([FromBody] SaveCorrectionFieldsVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess) return Forbid();
            try
            {
                await _valuerInboxService.SaveCorrectionFieldsAsync(vm, currentUser);
                return Json(new { success = true, count = vm.FieldKeys?.Count ?? 0 });
            }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSectionDecision([FromBody] QuickSectionDecisionVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess) return Forbid();
            try { await _valuerInboxService.SaveQuickSectionDecisionAsync(vm, currentUser); return Json(new { success = true }); }
            catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> ClientEvidenceFile(long attrId, long attrFileId, string fileName)
        {
            var model = await LoadAuthorisedReviewAsync(attrId);
            if (model == null) return Forbid();
            var file = model.EvidenceFiles.FirstOrDefault(x => x.AttrFileId == attrFileId &&
                string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase));
            return SendEvidenceFile(file?.FilePath, file?.FileName, file?.FileType);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEvidenceZip(long attrId, string kind = "client")
        {
            var model = await LoadAuthorisedReviewAsync(attrId);
            if (model == null) return Forbid();
            var files = string.Equals(kind, "inspection", StringComparison.OrdinalIgnoreCase)
                ? model.PhysicalInspectionEvidenceFiles.Select(x => new { x.FilePath, x.FileName }).ToList()
                : model.EvidenceFiles.Select(x => new { x.FilePath, x.FileName }).ToList();
            await using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                foreach (var file in files.Where(x => !string.IsNullOrWhiteSpace(x.FilePath) && System.IO.File.Exists(x.FilePath)))
                {
                    var entry = archive.CreateEntry(Path.GetFileName(file.FileName), CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await using var source = System.IO.File.OpenRead(file.FilePath!);
                    await source.CopyToAsync(entryStream);
                }
            }
            return File(output.ToArray(), "application/zip", $"{model.AttrNo}_{kind}_evidence.zip");
        }

        private async Task<ValuerReviewPageVm?> LoadAuthorisedReviewAsync(long attrId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess) return null;
            try { return await _valuerInboxService.OpenReviewAsync(attrId, currentUser); }
            catch { return null; }
        }

        private IActionResult SendEvidenceFile(string? path, string? fileName, string? contentType)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return NotFound("Evidence file was not found.");
            return PhysicalFile(path, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(path) : fileName, enableRangeProcessing: true);
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
