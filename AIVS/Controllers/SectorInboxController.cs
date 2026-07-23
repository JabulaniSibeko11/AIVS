using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers
{
    [Authorize]
    public class SectorInboxController : Controller
    {
        private readonly ISectorInboxService _sectorInboxService;
        private readonly IUserManagementService _userManagementService;
        private readonly IEmailService _emailService;

        public SectorInboxController(
            ISectorInboxService sectorInboxService,
            IUserManagementService userManagementService,
            IEmailService emailService)
        {
            _sectorInboxService = sectorInboxService;
            _userManagementService = userManagementService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? sector)
        {
            var model = await _sectorInboxService.BuildSectorInboxAsync(sector);
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess || currentUser.UserId == null)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS user access could not be verified.";
                return View(model);
            }

            var role = NormalizeRole(currentUser.Role);

            model.CurrentUserCanAssignToValuer =
                role == "SECTOR MANAGER" ||
                role == "VALUATION ADMIN" ||
                role == "EXECUTIVE" ||
                role == "SYSTEM ADMIN" ||
                role == "ADMIN" ||
                role == "ADMINISTRATOR" ||
                role == "IT MANAGER" ||
                role == "MANAGER";

            if (model.CurrentUserCanAssignToValuer)
            {
                model.Valuers = await _userManagementService.GetValuersAsync(sector);
            }

            ViewBag.CurrentAivsRole = currentUser.Role;
            ViewBag.CurrentAivsRoleNormalized = role;
            ViewBag.CurrentAivsUser = currentUser.FullName ?? currentUser.Username;

            return View(model);
        }
        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

            return role
                .Replace('\u00A0', ' ')
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim()
                .ToUpperInvariant();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSelectedToMe(List<long> selectedAttrIds, string? sector)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess || currentUser.UserId == null)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS user access could not be verified.";
                return RedirectToAction(nameof(Index), new { sector });
            }

            try
            {
                var result = await _sectorInboxService.AssignSelectedToMeAsync(
                    selectedAttrIds,
                    currentUser.UserId.Value,
                    currentUser.Username ?? User.Identity?.Name ?? "",
                    currentUser.FullName ?? User.Identity?.Name ?? "",
                    currentUser.Email,
                    currentUser.Role);

                if (!string.IsNullOrWhiteSpace(currentUser.Email))
                {
                    await _emailService.SendSelfAssignmentEmailAsync(
                        currentUser.Email,
                        currentUser.FullName ?? currentUser.Username ?? "Valuer",
                        result.Sector ?? sector ?? "",
                        result.AssignedCount,
                        result.AssignedReferences);
                }

                TempData["Success"] = $"{result.AssignedCount} item(s) assigned to your inbox successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { sector });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSelectedToValuer(
    List<long> selectedAttrIds,
    int selectedValuerUserId,
    string? sector)
        {
            var currentUser = await _userManagementService.GetCurrentUserAsync(User);

            if (!currentUser.HasAccess || currentUser.UserId == null)
            {
                TempData["Error"] = currentUser.AccessMessage ?? "Your AIVS user access could not be verified.";
                return RedirectToAction(nameof(Index), new { sector });
            }

            var managerRole = NormalizeRole(currentUser.Role);

            var canAssignToValuer =
                managerRole == "SECTOR MANAGER" ||
                managerRole == "VALUATION ADMIN" ||
                managerRole == "EXECUTIVE" ||
                managerRole == "SYSTEM ADMIN" ||
                managerRole == "ADMIN" ||
                managerRole == "ADMINISTRATOR" ||
                managerRole == "IT MANAGER" ||
                managerRole == "MANAGER";

            if (!canAssignToValuer)
            {
                TempData["Error"] = "You do not have permission to assign records to another valuer.";
                return RedirectToAction(nameof(Index), new { sector });
            }

            if (selectedValuerUserId <= 0)
            {
                TempData["Error"] = "Please select a valuer.";
                return RedirectToAction(nameof(Index), new { sector });
            }

            try
            {
                var valuers = await _userManagementService.GetValuersAsync(sector);
                var selectedValuer = valuers.FirstOrDefault(x => x.UserId == selectedValuerUserId);

                if (selectedValuer == null)
                {
                    TempData["Error"] = "The selected valuer could not be found.";
                    return RedirectToAction(nameof(Index), new { sector });
                }

                var result = await _sectorInboxService.AssignSelectedToValuerAsync(
                    selectedAttrIds,
                    selectedValuer.UserId,
                    selectedValuer.Username,
                    selectedValuer.FullName,
                    selectedValuer.Email,
                    selectedValuer.Role,
                    currentUser.UserId.Value,
                    currentUser.Username ?? User.Identity?.Name ?? "",
                    currentUser.FullName ?? User.Identity?.Name ?? "",
                    currentUser.Role);

                if (!string.IsNullOrWhiteSpace(selectedValuer.Email))
                {
                    await _emailService.SendManagerAssignmentEmailAsync(
                        selectedValuer.Email,
                        selectedValuer.FullName,
                        currentUser.FullName ?? currentUser.Username ?? "Sector Manager",
                        result.Sector ?? sector ?? "",
                        result.AssignedCount,
                        result.AssignedReferences);
                }

                TempData["Success"] = $"{result.AssignedCount} item(s) assigned to {selectedValuer.FullName}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { sector });
        }
        [HttpGet]
        public IActionResult Details(long id)
        {
            return Content($"Details page will be built next. Attr_ID: {id}");
        }
    }
}