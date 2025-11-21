namespace Contract_Monthly_Claim_System.Models
{
    public class ClaimItem
    {
        public int ClaimItemId { get; set; }
        public int ClaimId { get; set; }
        public int ModuleId { get; set; }
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Claim Claim { get; set; } = null!;
        public virtual Module Module { get; set; } = null!;
    }
}
