namespace Contract_Monthly_Claim_System.Models.View
{
    public class ReportViewModel
    {
        public string ReportTitle { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class MonthlyReportViewModel : ReportViewModel
    {
        public DateTime ReportMonth { get; set; }
        public List<LecturerSummaryViewModel> LecturerSummaries { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public decimal TotalHours { get; set; }
        public int TotalClaims { get; set; }
    }

    public class LecturerSummaryViewModel
    {
        public string LecturerName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageHourlyRate { get; set; }
    }

}
