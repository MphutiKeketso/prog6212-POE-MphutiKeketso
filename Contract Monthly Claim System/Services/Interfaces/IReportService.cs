using Contract_Monthly_Claim_System.Models.View;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IReportService
    {
        Task<MonthlyReportViewModel> GenerateMonthlyReportAsync(DateTime month, int userId);
        Task<byte[]> ExportReportAsync(string reportType, string format, Dictionary<string, object> parameters, int userId);
        Task<LecturerSummaryViewModel> GenerateLecturerReportAsync(int? lecturerId, DateTime fromDate, DateTime toDate, int userId);
        Task<ReportViewModel> GenerateProgrammeReportAsync(int? programmeId, DateTime fromDate, DateTime toDate, int userId);
    }
}
