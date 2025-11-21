namespace Contract_Monthly_Claim_System.Models
{
    public class Module
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string ModuleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProgrammeId { get; set; }
        public decimal HourlyRate { get; set; }
        public int CreditHours { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Programme Programme { get; set; } = null!;
        public virtual ICollection<ClaimItem> ClaimItems { get; set; } = new List<ClaimItem>();
        public virtual ICollection<LecturerModule> LecturerModules { get; set; } = new List<LecturerModule>();
    }
}
