namespace Contract_Monthly_Claim_System.Models.View
{
    public class CoordinatorDashboardViewModel
    {
        public string CoordinatorName { get; set; } = string.Empty;
        public List<string> ManagedProgrammes { get; set; } = new();
        public List<PendingClaimForApprovalViewModel> PendingClaims { get; set; } = new();
        public CoordinatorStatistics Statistics { get; set; } = new();
    }

    public class ManagerDashboardViewModel
    {
        public string ManagerName { get; set; } = string.Empty;
        public List<PendingClaimForApprovalViewModel> PendingClaims { get; set; } = new();
        public ManagerStatistics Statistics { get; set; } = new();
    }

    public class PendingClaimForApprovalViewModel
    {
        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string LecturerEmail { get; set; } = string.Empty;
        public DateTime ClaimMonth { get; set; }
        public DateTime SubmissionDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public int DocumentCount { get; set; }
        public List<string> Programmes { get; set; } = new();
        public List<string> Modules { get; set; } = new();
        public List<ClaimItemSummary> Items { get; set; } = new();
        public List<DocumentSummary> Documents { get; set; } = new();
        public int DaysPending { get; set; }
        public string Priority { get; set; } = string.Empty;
        public bool HasRequiredDocuments { get; set; }
    }

    public class ClaimItemSummary
    {
        public string ModuleCode { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class DocumentSummary
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class CoordinatorStatistics
    {
        public int PendingReviewCount { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public decimal TotalAmountPending { get; set; }
        public int OverdueClaims { get; set; }
    }

    public class ManagerStatistics
    {
        public int PendingReviewCount { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public decimal TotalAmountPending { get; set; }
        public decimal TotalAmountApproved { get; set; }
        public int OverdueClaims { get; set; }
    }
}
