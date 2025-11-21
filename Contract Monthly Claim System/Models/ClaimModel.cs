using System.Reflection.Metadata;

namespace Contract_Monthly_Claim_System.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int LecturerId { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public DateTime ClaimMonth { get; set; }
        public DateTime SubmissionDate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
        public string Notes { get; set; } = string.Empty;

        // Coordinator approval fields
        public DateTime? CoordinatorApprovalDate { get; set; }
        public int? CoordinatorId { get; set; }
        public string CoordinatorNotes { get; set; } = string.Empty;

        // Manager approval fields
        public DateTime? ManagerApprovalDate { get; set; }
        public int? ManagerId { get; set; }
        public string ManagerNotes { get; set; } = string.Empty;

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Lecturer Lecturer { get; set; } = null!;
        public virtual ProgrammeCoordinator? Coordinator { get; set; }
        public virtual AcademicManager? Manager { get; set; }
        public virtual ICollection<ClaimItem> ClaimItems { get; set; } = new List<ClaimItem>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
    }
}
