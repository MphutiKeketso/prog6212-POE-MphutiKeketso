using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize(Roles = "SystemAdministrator")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IProgrammeService _programmeService;
        private readonly IModuleService _moduleService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IUserService userService,
            IProgrammeService programmeService,
            IModuleService moduleService,
            ILogger<AdminController> logger)
        {
            _userService = userService;
            _programmeService = programmeService;
            _moduleService = moduleService;
            _logger = logger;
        }

        // GET: Admin
        public IActionResult Index()
        {
            return View();
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users(UserSearchDto searchDto)
        {
            var users = await _userService.SearchUsersAsync(searchDto);
            ViewBag.SearchDto = searchDto;

            return View(users);
        }

        // GET: Admin/Programmes
        public async Task<IActionResult> Programmes()
        {
            var programmes = await _programmeService.GetAllProgrammesAsync();
            return View(programmes);
        }

        // GET: Admin/Modules
        public async Task<IActionResult> Modules(int? programmeId)
        {
            var modules = await _moduleService.GetModulesAsync(programmeId);
            ViewBag.ProgrammeId = programmeId;

            return View(modules);
        }
    }
}
