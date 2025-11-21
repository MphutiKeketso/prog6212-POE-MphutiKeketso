namespace Contract_Monthly_Claim_System.Models.View
{
    public class ClaimStatistics
    {
        public int TotalItems { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageHourlyRate { get; set; }
        public decimal MinHourlyRate { get; set; }
        public decimal MaxHourlyRate { get; set; }
        public int UniqueModules { get; set; }
    }

    public class ClaimFilterOptions
    {
        public List<ClaimStatus> AvailableStatuses { get; set; } = new();
        public List<Programme> AvailableProgrammes { get; set; } = new();
        public List<Lecturer> AvailableLecturers { get; set; } = new();
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }

    public class ClaimSummaryViewModel
    {
        public int ClaimId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public DateTime ClaimMonth { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; }
        public int ItemCount { get; set; }
        public int DocumentCount { get; set; }
        public bool HasRequiredDocuments { get; set; }
        public DateTime SubmissionDate { get; set; }
        public int DaysPending { get; set; }
    }
}
