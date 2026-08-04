using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AIVS.Security;

namespace AIVS.Controllers
{
    [Authorize(Policy = AivsPolicyNames.AccessAivs)]
    public class AccountController : Controller
    {
        private readonly IUserManagementService _userManagement;

        public AccountController(IUserManagementService userManagement)
        {
            _userManagement = userManagement;
        }

        [HttpGet]
        public async Task<IActionResult> Me()
        {
            var currentUser = await _userManagement.GetCurrentUserAsync(User);

            return View(currentUser);
        }
    }
}
