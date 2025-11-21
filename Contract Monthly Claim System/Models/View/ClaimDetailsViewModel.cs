namespace Contract_Monthly_Claim_System.Models.View
{
    public class ClaimDetailsViewModel
    {
        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public DateTime ClaimMonth { get; set; }
        public DateTime SubmissionDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<ClaimItemViewModel> ClaimItems { get; set; } = new();
        public List<DocumentViewModel> Documents { get; set; } = new();
        public List<StatusHistoryViewModel> StatusHistory { get; set; } = new();
        public ApprovalDetailsViewModel? CoordinatorApproval { get; set; }
        public ApprovalDetailsViewModel? ManagerApproval { get; set; }
        public bool CanEdit { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
    }
}
