using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;

namespace Contract_Monthly_Claim_System.Controllers
{
    // Access limited to HR or Admins
    [Authorize(Roles = "SystemAdministrator, AcademicManager")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. HR Dashboard / Lecturer Management
        public async Task<IActionResult> Index()
        {
            var lecturers = await _context.Lecturers
                .Select(l => new LecturerViewModel
                {
                    UserId = l.UserId,
                    FullName = l.FirstName + " " + l.LastName,
                    Email = l.Email,
                    EmployeeNumber = l.EmployeeNumber,
                    HourlyRate = l.DefaultHourlyRate
                }).ToListAsync();

            return View(lecturers);
        }

        // 2. Update Lecturer Data
        [HttpPost]
        public async Task<IActionResult> UpdateRate(int id, decimal newRate)
        {
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer != null)
            {
                lecturer.DefaultHourlyRate = newRate;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 3. AUTOMATION: Generate Payment Report (Invoice Generation)
        public async Task<IActionResult> GeneratePaymentReport()
        {
            // LINQ to generate summary of Approved Claims ready for payment
            var report = await _context.Claims
                .Where(c => c.Status == ClaimStatus.ManagerApproved) // Ready for HR
                .Select(c => new PaymentReportItem
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.FirstName + " " + c.Lecturer.LastName,
                    EmployeeNo = c.Lecturer.EmployeeNumber,
                    Month = c.ClaimMonth.ToString("MMM yyyy"),
                    TotalHours = c.TotalHours,
                    TotalPayable = c.TotalAmount,
                    BankDetails = c.Lecturer.BankAccountNumber
                })
                .ToListAsync();

            return View("PaymentReport", report);
        }

        // 4. Process Payment (Simulated)
        [HttpPost]
        public async Task<IActionResult> ProcessPayments(List<int> claimIds)
        {
            var claims = await _context.Claims.Where(c => claimIds.Contains(c.ClaimId)).ToListAsync();
            foreach (var claim in claims)
            {
                claim.Status = ClaimStatus.Paid;
                // In a real scenario, this would call an external Banking API
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GeneratePaymentReport));
        }
    }

    public class LecturerViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string EmployeeNumber { get; set; }
        public decimal HourlyRate { get; set; }
    }

    public class PaymentReportItem
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; }
        public string EmployeeNo { get; set; }
        public string Month { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalPayable { get; set; }
        public string BankDetails { get; set; }
    }



}