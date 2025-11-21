using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IModuleService _moduleService;
        private readonly IDocumentService _documentService;
        private readonly IUserService _userService;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(
            IClaimService claimService,
            IModuleService moduleService,
            IDocumentService documentService,
            IUserService userService,
            ILogger<ClaimsController> logger)
        {
            _claimService = claimService;
            _moduleService = moduleService;
            _documentService = documentService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Claims
        public async Task<IActionResult> Index(ClaimSearchDto searchDto)
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var claims = await _claimService.SearchClaimsAsync(searchDto, currentUser.UserId);

            ViewBag.SearchDto = searchDto;
            return View(claims);
        }

        // GET: Claims/Details/5
        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // 1. Get the data from the service
            var claim = await _claimService.GetClaimDetailsAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // 2. Get the current user to check permissions
            var currentUser = await _userService.GetCurrentUserAsync(User);

            // 3. Set permissions manually here (replacing the 'NotImplemented' builder)
            if (currentUser != null)
            {
                // Coordinators can approve if status is 'Submitted' or 'UnderReview'
                if (currentUser.UserType == UserType.ProgrammeCoordinator)
                {
                    claim.CanApprove = claim.Status == ClaimStatus.Submitted ||
                                       claim.Status == ClaimStatus.UnderCoordinatorReview;
                    claim.CanReject = claim.CanApprove;
                }
                // Managers can approve if status is 'CoordinatorApproved' or 'UnderManagerReview'
                else if (currentUser.UserType == UserType.AcademicManager)
                {
                    claim.CanApprove = claim.Status == ClaimStatus.CoordinatorApproved ||
                                       claim.Status == ClaimStatus.UnderManagerReview;
                    claim.CanReject = claim.CanApprove;
                }
            }

            // 4. Return the view
            return View(claim);
        }

        // GET: Claims/Create
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var availableModules = await _moduleService.GetLecturerModulesAsync(currentUser.UserId);

            var viewModel = new CreateClaimViewModel
            {
                ClaimMonth = DateTime.Today.AddDays(-DateTime.Today.Day + 1), // First day of current month
                SubmissionDate = DateTime.Today,
                AvailableModules = availableModules
            };

            return View(viewModel);
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create(CreateClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableModules = await _moduleService.GetLecturerModulesAsync(
                    (await _userService.GetCurrentUserAsync(User)).UserId);
                return View(model);
            }

            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                var claimId = await _claimService.CreateClaimAsync(model, currentUser.UserId);

                // Handle document uploads
                if (model.Documents?.Any() == true)
                {
                    await _documentService.UploadDocumentsAsync(claimId, model.Documents);
                }

                TempData["SuccessMessage"] = "Claim submitted successfully!";
                return RedirectToAction(nameof(Details), new { id = claimId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claim for user {UserId}",
                    (await _userService.GetCurrentUserAsync(User)).UserId);
                ModelState.AddModelError("", "An error occurred while creating the claim. Please try again.");

                model.AvailableModules = await _moduleService.GetLecturerModulesAsync(
                    (await _userService.GetCurrentUserAsync(User)).UserId);
                return View(model);
            }
        }

        // GET: Claims/Edit/5
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id)
        {
            var claim = await _claimService.GetClaimForEditAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            var currentUser = await _userService.GetCurrentUserAsync(User);

            // Check if user can edit this claim
            if (claim.LecturerId != currentUser.UserId ||
                claim.Status != ClaimStatus.Draft && claim.Status != ClaimStatus.CoordinatorRejected)
            {
                return Forbid();
            }

            var viewModel = await BuildEditClaimViewModel(claim, currentUser.UserId);
            return View(viewModel);
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id, CreateClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                model.AvailableModules = await _moduleService.GetLecturerModulesAsync(currentUser.UserId);
                return View(model);
            }

            try
            {
                await _claimService.UpdateClaimAsync(id, model);

                TempData["SuccessMessage"] = "Claim updated successfully!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating claim {ClaimId}", id);
                ModelState.AddModelError("", "An error occurred while updating the claim. Please try again.");

                var currentUser = await _userService.GetCurrentUserAsync(User);
                model.AvailableModules = await _moduleService.GetLecturerModulesAsync(currentUser.UserId);
                return View(model);
            }
        }

        // POST: Claims/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(ApprovalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid approval data." });
            }

            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                await _claimService.ProcessApprovalAsync(model, currentUser.UserId);

                return Json(new { success = true, message = "Claim processed successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing approval for claim {ClaimId}", model.ClaimId);
                return Json(new { success = false, message = "An error occurred while processing the approval." });
            }
        }

        // GET: Claims/PendingApprovals
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> PendingApprovals()
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var pendingClaims = await _claimService.GetPendingApprovalsAsync(currentUser.UserId);

            return View(pendingClaims);
        }

        // ==========================================================
        // == METHOD FROM SAMPLE 2 (MERGED)
        // ==========================================================

        // GET: Claims/Track/5
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Track(int id)
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var claim = await _claimService.GetEnhancedClaimDetailsAsync(id, currentUser.UserId);

            if (claim == null)
                return NotFound();

            // Verify lecturer owns this claim
            if (claim.LecturerName != currentUser.FullName)
                return Forbid();

            return View(claim);
        }

        // ==========================================================
        // == PRIVATE HELPER METHODS
        // ==========================================================

        private async Task<ClaimDetailsViewModel> BuildClaimDetailsViewModel(Claim claim, User currentUser)
        {
            // Implementation details for building the view model
            return new ClaimDetailsViewModel(); // Simplified for brevity
        }

        private async Task<CreateClaimViewModel> BuildEditClaimViewModel(Claim claim, int userId)
        {
            var availableModules = await _moduleService.GetLecturerModulesAsync(userId);

            return new CreateClaimViewModel
            {
                ClaimMonth = claim.ClaimMonth,
                SubmissionDate = claim.SubmissionDate,
                Notes = claim.Notes,
                ClaimItems = claim.ClaimItems.Select(ci => new ClaimItemViewModel
                {
                    ModuleId = ci.ModuleId,
                    ModuleName = ci.Module.ModuleName,
                    ModuleCode = ci.Module.ModuleCode,
                    HoursWorked = ci.HoursWorked,
                    HourlyRate = ci.HourlyRate,
                    TotalAmount = ci.TotalAmount,
                    Description = ci.Description,
                    WorkDate = ci.WorkDate
                }).ToList(),
                AvailableModules = availableModules
            };
        }
    }
}