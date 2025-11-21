using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
    public class AcademicManager : User
    {
        public string Division { get; set; } = string.Empty;
        public decimal ApprovalLimit { get; set; }

        // Navigation properties
        public virtual ICollection<Claim> ManagerApprovedClaims { get; set; } = new List<Claim>();
    }
}
