using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Route("api/programmes")]
    [ApiController]
    [Authorize]
    public class ProgrammesApiController : ControllerBase
    {
        private readonly IProgrammeService _programmeService;
        private readonly IModuleService _moduleService;

        public ProgrammesApiController(IProgrammeService programmeService, IModuleService moduleService)
        {
            _programmeService = programmeService;
            _moduleService = moduleService;
        }

        // GET: api/programmes
        [HttpGet]
        public async Task<IActionResult> GetAllProgrammes()
        {
            var programmes = await _programmeService.GetAllProgrammesAsync();
            return Ok(programmes);
        }

        // GET: api/programmes/{id}/modules
        [HttpGet("{id}/modules")]
        public async Task<IActionResult> GetModulesByProgramme(int id)
        {
            var modules = await _moduleService.GetModulesByProgrammeAsync(id);
            return Ok(modules);
        }
    }
}