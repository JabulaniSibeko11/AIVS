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
        private readonly IProcessorFileService _processorFiles;
        public ValuerInboxController(
            IValuerInboxService valuerInboxService,
            IUserManagementService userManagementService,
            AttributesDbContext context,
            IProcessorFileService processorFiles)

        {
            _valuerInboxService = valuerInboxService;
            _userManagementService = userManagementService;
            _context = context;
            _processorFiles = processorFiles;
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
        public async Task<IActionResult> ResolveRatingDifference([FromBody] ResolveRatingDifferenceVm vm)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            if (!currentUser.HasAccess) return Forbid();

            try
            {
                await _valuerInboxService.ResolveRatingDifferenceAsync(vm, currentUser);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProcessorEvidence(long attrId, List<IFormFile> files, string? evidenceComment)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            var isAjax = string.Equals(Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                         || Request.Headers.Accept.Any(x => x != null && x.Contains("application/json", StringComparison.OrdinalIgnoreCase));

            try
            {
                // OpenReviewAsync performs the current processor assignment/access check.
                await _valuerInboxService.OpenReviewAsync(attrId, currentUser);

                if (files == null || files.Count == 0)
                    throw new InvalidOperationException("Select at least one Internal Processor Evidence file.");

                await _processorFiles.UploadEvidenceAsync(attrId, files, evidenceComment, "Processor Review", currentUser);

                if (isAjax)
                    return Json(new { success = true, count = files.Count });

                TempData["Success"] = "Processor evidence uploaded successfully.";
            }
            catch (Exception ex)
            {
                if (isAjax)
                    return BadRequest(new { success = false, message = ex.Message });

                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id = attrId });
        }

        [HttpGet]
        public async Task<IActionResult> ProcessorEvidenceFile(long attrId, long evidenceId, bool download = false)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);
            try
            {
                await _valuerInboxService.OpenReviewAsync(attrId, currentUser);
                var file = await _processorFiles.GetEvidenceFileAsync(evidenceId);
                if (file == null) return NotFound("Processor evidence file was not found.");
                return download
                    ? PhysicalFile(file.Value.Path, file.Value.ContentType, file.Value.FileName, enableRangeProcessing: true)
                    : PhysicalFile(file.Value.Path, file.Value.ContentType, enableRangeProcessing: true);
            }
            catch
            {
                return Forbid();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ClientEvidenceFile(long attrId, string evidenceKey, bool download = false)
        {
            var model = await LoadAuthorisedReviewAsync(attrId);
            if (model == null) return Forbid();

            if (string.IsNullOrWhiteSpace(evidenceKey))
                return BadRequest("Evidence reference is missing.");

            var file = model.EvidenceFiles.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.EvidenceKey) &&
                string.Equals(x.EvidenceKey, evidenceKey, StringComparison.OrdinalIgnoreCase));

            if (file == null)
                return NotFound("The requested evidence file is no longer available for this attribute submission.");

            return SendEvidenceFile(file.FilePath, file.FileName, file.FileType, download);
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

        private IActionResult SendEvidenceFile(string? path, string? fileName, string? contentType, bool download = false)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return NotFound("Evidence file was not found on the Attributes evidence folder.");

            var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType;

            if (download)
            {
                return PhysicalFile(
                    path,
                    resolvedContentType,
                    string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(path) : fileName,
                    enableRangeProcessing: true);
            }

            // No download file-name means supported files (PDF/images/text) open inline in the browser.
            return PhysicalFile(path, resolvedContentType, enableRangeProcessing: true);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteInspection(long inspectionRequestId, long attrId)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS access could not be verified.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _valuerInboxService.CompleteInspectionAsync(inspectionRequestId, currentUser);
                TempData["Success"] =
                    "Physical inspection completed. The inspection evidence is now part of the review and the task can continue to OVVIO submission / QA.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id = attrId });
        }

        [HttpGet]
        public async Task<IActionResult> PhysicalInspectionEvidenceFile(long id, bool download = false)
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

            if (download)
                return File(bytes, contentType, fileName);

            Response.Headers.ContentDisposition = $"inline; filename=\"{fileName.Replace("\"", string.Empty)}\"";
            return File(bytes, contentType);
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
