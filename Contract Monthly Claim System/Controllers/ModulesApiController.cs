using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Route("api/modules")]
    [ApiController]
    [Authorize]
    public class ModulesApiController : ControllerBase
    {
        private readonly IModuleService _moduleService;

        public ModulesApiController(IModuleService moduleService)
        {
            _moduleService = moduleService;
        }

        // GET: api/modules/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetModule(int id)
        {
            var module = await _moduleService.GetModuleByIdAsync(id);

            if (module == null)
            {
                return NotFound();
            }

            return Ok(module);
        }
    }
}