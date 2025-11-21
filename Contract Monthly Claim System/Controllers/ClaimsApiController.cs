using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ClaimsApiController : ControllerBase
    {
        private readonly IClaimService _claimService;

        public ClaimsApiController(IClaimService claimService)
        {
            _claimService = claimService;
        }

        [HttpGet("status/{id}")]
        public async Task<ActionResult<ApiResponseDto<ClaimStatus>>> GetClaimStatus(int id)
        {
            try
            {
                var status = await _claimService.GetClaimStatusAsync(id);
                return Ok(new ApiResponseDto<ClaimStatus>
                {
                    Success = true,
                    Data = status
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDto<ClaimStatus>
                {
                    Success = false,
                    Message = "Error retrieving claim status.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("calculate-total")]
        public ActionResult<ApiResponseDto<decimal>> CalculateClaimTotal([FromBody] List<ClaimItemViewModel> items)
        {
            try
            {
                var total = items.Sum(item => item.HoursWorked * item.HourlyRate);
                return Ok(new ApiResponseDto<decimal>
                {
                    Success = true,
                    Data = total
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponseDto<decimal>
                {
                    Success = false,
                    Message = "Error calculating total.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
