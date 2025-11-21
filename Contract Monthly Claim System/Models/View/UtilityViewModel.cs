namespace Contract_Monthly_Claim_System.Models.View
{
    public class NotificationViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // success, warning, info, danger
        public DateTime CreatedDate { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }

    public class StatusHistoryViewModel
    {
        public ClaimStatus PreviousStatus { get; set; }
        public ClaimStatus NewStatus { get; set; }
        public DateTime StatusChangeDate { get; set; }
        public string ChangedByName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public string StatusChangeDisplay { get; set; } = string.Empty;
    }

    public class RecentClaimViewModel
    {
        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public DateTime ClaimMonth { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
    }

    public class ModuleSelectViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ModuleCode { get; set; } = string.Empty;
        public string ProgrammeName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public bool IsAssigned { get; set; }
    }
}
