using AIVS.Models.ViewModels.SectorManager;
using AIVS.Models.ViewModels.SeniorManager;
using AIVS.Security;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers;

[Authorize(Policy = AivsPolicyNames.SeniorManagerQa)]
public class SeniorManagerQaController : Controller
{
    private readonly ISectorManagerQaService _qaService;
    private readonly IUserManagementService _users;
    private readonly IProcessorFileService _processorFiles;

    public SeniorManagerQaController(ISectorManagerQaService qaService, IUserManagementService users, IProcessorFileService processorFiles)
    {
        _qaService = qaService;
        _users = users;
        _processorFiles = processorFiles;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetCurrentUserAsync(User);
        return View(await _qaService.GetSeniorManagerInboxAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(long id)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            await _qaService.ClaimSeniorManagerQaAsync(id, user);
            TempData["Success"] = "The Senior Manager QA item was assigned to you.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            return View(await _qaService.GetSeniorManagerDetailsAsync(id, user));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> EvidenceFile(long id, long attrFileId, string fileName)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            var model = await _qaService.GetSeniorManagerDetailsAsync(id, user);
            var file = model.EvidenceFiles.FirstOrDefault(x =>
                x.AttrFileId == attrFileId &&
                string.Equals(x.FileName, fileName, StringComparison.OrdinalIgnoreCase));

            if (file == null || string.IsNullOrWhiteSpace(file.FilePath) || !System.IO.File.Exists(file.FilePath))
                return NotFound("Evidence file was not found.");

            return PhysicalFile(
                file.FilePath,
                string.IsNullOrWhiteSpace(file.FileType) ? "application/octet-stream" : file.FileType,
                file.FileName,
                enableRangeProcessing: true);
        }
        catch
        {
            return Forbid();
        }
    }


    [HttpGet]
    public async Task<IActionResult> ProcessorEvidenceFile(long id, long evidenceId, bool download = false)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            await _qaService.GetSeniorManagerDetailsAsync(id, user);
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
    public async Task<IActionResult> Approve(SeniorManagerQaDecisionVm vm)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            await _qaService.ApproveSeniorManagerQaAsync(vm, user);
            TempData["Success"] = "Final approval completed. The attributes were inserted into OVVIO staging and the client approval notice/email was generated.";
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
    public async Task<IActionResult> ReturnToSectorManager(SeniorManagerQaDecisionVm vm)
    {
        var user = await _users.GetCurrentUserAsync(User);
        try
        {
            await _qaService.ReturnToSectorManagerAsync(vm, user);
            TempData["Success"] = "The QA review was returned to the Sector Manager.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = vm.QaId });
        }
    }
}
