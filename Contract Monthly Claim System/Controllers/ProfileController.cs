using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IUserService userService, ILogger<ProfileController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: /Profile/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateUserViewModel());
        }

        // POST: /Profile/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _userService.CreateUserAsync(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Profile created successfully! You can now log in.";
                    // Redirect to Login page (adjust area/controller if using default Identity)
                    return RedirectToPage("/Account/Login", new { area = "Identity" });
                }

                ModelState.AddModelError(string.Empty, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating profile");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred.");
            }

            return View(model);
        }
    }
}