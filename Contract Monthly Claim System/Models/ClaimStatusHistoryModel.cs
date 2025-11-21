using System.ComponentModel.DataAnnotations;
namespace Contract_Monthly_Claim_System.Models
{
    public class ClaimStatusHistory
    {
        [Key]
        public int StatusHistoryId { get; set; }
        public int ClaimId { get; set; }
        public ClaimStatus PreviousStatus { get; set; }
        public ClaimStatus NewStatus { get; set; }
        public DateTime StatusChangeDate { get; set; } = DateTime.UtcNow;
        public int ChangedByUserId { get; set; }
        public string Comments { get; set; } = string.Empty;
        public string SystemNotes { get; set; } = string.Empty;

        // Navigation properties
        public virtual Claim Claim { get; set; } = null!;
        public virtual User ChangedBy { get; set; } = null!;
    }
}
