namespace Contract_Monthly_Claim_System.Models.View
{
    public class CreateClaimViewModel
    {
        public DateTime ClaimMonth { get; set; }
        public DateTime SubmissionDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public List<ClaimItemViewModel> ClaimItems { get; set; } = new();
        public List<IFormFile> Documents { get; set; } = new();
        public List<ModuleSelectViewModel> AvailableModules { get; set; } = new();
    }

    public class ClaimItemViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ModuleCode { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
    }

    
}
