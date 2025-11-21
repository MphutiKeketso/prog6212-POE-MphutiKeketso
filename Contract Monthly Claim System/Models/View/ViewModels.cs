namespace Contract_Monthly_Claim_System.Models.View
{
    public class EnhancedClaimDetailsViewModel : ClaimDetailsViewModel
    {
        // Progress tracking
        public int ProgressPercentage { get; set; }
        public string CurrentStage { get; set; } = string.Empty;
        public List<ClaimProgressStep> ProgressSteps { get; set; } = new();

        // Approval information
        public ApprovalInfo? CoordinatorApprovalInfo { get; set; }
        public ApprovalInfo? ManagerApprovalInfo { get; set; }

        // Timeline
        public List<StatusTimelineItem> Timeline { get; set; } = new();
    }

    public class ClaimProgressStep
    {
        public string StepName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Icon { get; set; } = string.Empty;
    }

    public class ApprovalInfo
    {
        public string ApproverName { get; set; } = string.Empty;
        public string ApproverRole { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string Decision { get; set; } = string.Empty; // "Approved" or "Rejected"
        public string Comments { get; set; } = string.Empty;
        public string ApproverEmail { get; set; } = string.Empty;
    }

    public class StatusTimelineItem
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string ColorClass { get; set; } = string.Empty;
    }
}
