using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVS.Controllers
{
    [Authorize]
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
