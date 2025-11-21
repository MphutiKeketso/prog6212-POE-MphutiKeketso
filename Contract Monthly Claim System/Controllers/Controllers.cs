using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contract_Monthly_Claim_System.Services.Implementation;


namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize]
    public class ApprovalController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(
            IClaimService claimService,
            IUserService userService,
            ILogger<ApprovalController> logger)
        {
            _claimService = claimService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Approval/CoordinatorDashboard
        [Authorize(Roles = "ProgrammeCoordinator")]
        public async Task<IActionResult> CoordinatorDashboard()
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var viewModel = await _claimService.GetCoordinatorDashboardAsync(currentUser.UserId);

            return View(viewModel);
        }

        // GET: Approval/ManagerDashboard
        [Authorize(Roles = "AcademicManager")]
        public async Task<IActionResult> ManagerDashboard()
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var viewModel = await _claimService.GetManagerDashboardAsync(currentUser.UserId);

            return View(viewModel);
        }

        // GET: Approval/ReviewClaim/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> ReviewClaim(int id)
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var claim = await _claimService.GetEnhancedClaimDetailsAsync(id, currentUser.UserId);

            if (claim == null)
                return NotFound();

            ViewBag.UserRole = currentUser.UserType.ToString();
            return View(claim);
        }

        // POST: Approval/ProcessApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> ProcessApproval(int claimId, string action, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Comments are required for approval/rejection.";
                return RedirectToAction(nameof(ReviewClaim), new { id = claimId });
            }

            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                var approvalModel = new ApprovalViewModel
                {
                    ClaimId = claimId,
                    Action = action,
                    Comments = comments,
                    NotifyLecturer = true
                };

                await _claimService.ProcessApprovalAsync(approvalModel, currentUser.UserId);

                TempData["SuccessMessage"] = $"Claim {action}d successfully!";

                // Redirect to appropriate dashboard
                if (currentUser.UserType == UserType.ProgrammeCoordinator)
                    return RedirectToAction(nameof(CoordinatorDashboard));
                else
                    return RedirectToAction(nameof(ManagerDashboard));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval for claim {ClaimId}", claimId);
                TempData["ErrorMessage"] = "An error occurred while processing the approval.";
                return RedirectToAction(nameof(ReviewClaim), new { id = claimId });
            }
        }
    }
}
