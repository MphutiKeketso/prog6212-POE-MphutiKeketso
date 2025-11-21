using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize(Roles = "ProgrammeCoordinator,AcademicManager,SystemAdministrator")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportService reportService,
            IUserService userService,
            ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/Monthly
        public async Task<IActionResult> Monthly(DateTime? month)
        {
            var reportMonth = month ?? DateTime.Today.AddDays(-DateTime.Today.Day + 1);
            var currentUser = await _userService.GetCurrentUserAsync(User);

            var report = await _reportService.GenerateMonthlyReportAsync(reportMonth, currentUser.UserId);

            return View(report);
        }

        // GET: Reports/Lecturer
        public async Task<IActionResult> Lecturer(int? lecturerId, DateTime? fromDate, DateTime? toDate)
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);

            // Set default date range if not provided
            fromDate ??= DateTime.Today.AddMonths(-3);
            toDate ??= DateTime.Today;

            var report = await _reportService.GenerateLecturerReportAsync(
                lecturerId, fromDate.Value, toDate.Value, currentUser.UserId);

            return View(report);
        }

        // GET: Reports/Programme
        public async Task<IActionResult> Programme(int? programmeId, DateTime? fromDate, DateTime? toDate)
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);

            // Set default date range if not provided
            fromDate ??= DateTime.Today.AddMonths(-3);
            toDate ??= DateTime.Today;

            var report = await _reportService.GenerateProgrammeReportAsync(
                programmeId, fromDate.Value, toDate.Value, currentUser.UserId);

            return View(report);
        }

        // POST: Reports/Export
        [HttpPost]
        public async Task<IActionResult> Export(string reportType, string format, Dictionary<string, object> parameters)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                var reportData = await _reportService.ExportReportAsync(reportType, format, parameters, currentUser.UserId);

                var contentType = format.ToLower() switch
                {
                    "pdf" => "application/pdf",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "csv" => "text/csv",
                    _ => "application/octet-stream"
                };

                var fileName = $"{reportType}_Report_{DateTime.Now:yyyyMMdd}.{format.ToLower()}";

                return File(reportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report: {ReportType}", reportType);
                return BadRequest("Error generating report. Please try again.");
            }
        }
    }
}
