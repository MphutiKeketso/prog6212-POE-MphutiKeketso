using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services.Implementation
{
    public interface IVerificationService
    {
        Task<List<string>> VerifyClaimRulesAsync(int claimId);
    }

    public class AutomatedVerificationService : IVerificationService
    {
        private readonly ApplicationDbContext _context;

        // Predefined Criteria / Policies
        private const decimal MAX_HOURS_PER_MONTH = 180;
        private const decimal MAX_CLAIM_AMOUNT = 50000;

        public AutomatedVerificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> VerifyClaimRulesAsync(int claimId)
        {
            var warnings = new List<string>();

            var claim = await _context.Claims
                .Include(c => c.ClaimItems)
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return warnings;

            // Rule 1: Check Hourly Rates against Contract
            foreach (var item in claim.ClaimItems)
            {
                // Fetch the official rate for this module
                var moduleRate = await _context.Modules
                    .Where(m => m.ModuleId == item.ModuleId)
                    .Select(m => m.HourlyRate)
                    .FirstOrDefaultAsync();

                if (item.HourlyRate != moduleRate)
                {
                    warnings.Add($"[Rate Mismatch] Item for Module {item.ModuleId} uses rate {item.HourlyRate} but system rate is {moduleRate}.");
                }
            }

            // Rule 2: Work Hours Cap
            if (claim.TotalHours > MAX_HOURS_PER_MONTH)
            {
                warnings.Add($"[Policy Breach] Total hours ({claim.TotalHours}) exceed monthly limit of {MAX_HOURS_PER_MONTH}.");
            }

            // Rule 3: High Value Claim
            if (claim.TotalAmount > MAX_CLAIM_AMOUNT)
            {
                warnings.Add($"[Audit Required] Claim amount {claim.TotalAmount:C} exceeds auto-approval threshold.");
            }

            // Rule 4: Duplicate Claims Check
            var duplicates = await _context.Claims
                .AnyAsync(c => c.LecturerId == claim.LecturerId
                             && c.ClaimMonth == claim.ClaimMonth
                             && c.ClaimId != claim.ClaimId);

            if (duplicates)
            {
                warnings.Add("[Duplicate] Another claim exists for this lecturer in this month.");
            }

            return warnings;
        }
    }
}