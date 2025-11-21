namespace Contract_Monthly_Claim_System.Models.View
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public DashboardStatsViewModel Stats { get; set; } = new();
        public List<RecentClaimViewModel> RecentClaims { get; set; } = new();
        public List<NotificationViewModel> Notifications { get; set; } = new();
        public List<PendingApprovalViewModel> PendingApprovals { get; set; } = new();
    }

    public class DashboardStatsViewModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public decimal TotalEarned { get; set; }
        public int RejectedClaims { get; set; }
        public decimal AverageClaimAmount { get; set; }
    }
}
