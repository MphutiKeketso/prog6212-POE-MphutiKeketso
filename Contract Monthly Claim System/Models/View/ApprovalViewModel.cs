namespace Contract_Monthly_Claim_System.Models.View
{
    public class ApprovalViewModel
    {
        public int ClaimId { get; set; }
        public string Action { get; set; } = string.Empty; // "approve" or "reject"
        public string Comments { get; set; } = string.Empty;
        public bool NotifyLecturer { get; set; } = true;
    }

    public class ApprovalDetailsViewModel
    {
        public DateTime ApprovalDate { get; set; }
        public string ApproverName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class PendingApprovalViewModel
    {
        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Priority { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
    }
}
