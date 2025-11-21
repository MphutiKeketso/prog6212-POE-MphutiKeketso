using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;
        private readonly IDocumentService _documentService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IClaimService claimService,
            IUserService userService,
            INotificationService notificationService,
            IDocumentService documentService,
            ILogger<HomeController> logger)
        {
            _claimService = claimService;
            _userService = userService;
            _notificationService = notificationService;
            _documentService = documentService;
            _logger = logger;
        }

        public async Task<IActionResult>
    Index()
        {
            // 1. Check if user is logged in
            if (!User.Identity.IsAuthenticated)
            {
                return View("Index_Guest"); // You'll need to create this simple landing page View or just return View() if your Index is safe for guests.
                                            // OR: return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var currentUser = await _userService.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                // Handle edge case: Identity exists but DB record is missing
                return View("Error", new ErrorViewModel { RequestId = "User record missing" });
            }

            // 2. Role-based Redirect
            if (User.IsInRole("ProgrammeCoordinator"))
            {
                return RedirectToAction("CoordinatorDashboard", "Approval");
            }
            if (User.IsInRole("AcademicManager"))
            {
                return RedirectToAction("ManagerDashboard", "Approval");
            }
            if (User.IsInRole("SystemAdministrator"))
            {
                return RedirectToAction("Index", "HR");
            }

            // 3. Default for Lecturers (The Standard Dashboard)
            var dashboardData = await BuildDashboardViewModel(currentUser);
            return View(dashboardData);
        }


        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private async Task<DashboardViewModel> BuildDashboardViewModel(User user)
        {
            var viewModel = new DashboardViewModel
            {
                UserName = user.FullName,
                UserType = user.UserType,
                Stats = await _claimService.GetDashboardStatsAsync(user.UserId),
                RecentClaims = await _claimService.GetRecentClaimsAsync(user.UserId, 5),
                Notifications = await _notificationService.GetRecentNotificationsAsync(user.UserId, 10)
            };

            // Add pending approvals for coordinators and managers
            if (user.UserType == UserType.ProgrammeCoordinator || user.UserType == UserType.AcademicManager)
            {
                viewModel.PendingApprovals = await _claimService.GetPendingApprovalsAsync(user.UserId);
            }

            return viewModel;
        }
    }
}
