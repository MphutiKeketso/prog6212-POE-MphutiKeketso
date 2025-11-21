using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
    public class ProgrammeCoordinator : User
    {
        public string Department { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Programme> ManagedProgrammes { get; set; } = new List<Programme>();
        public virtual ICollection<Claim> CoordinatorApprovedClaims { get; set; } = new List<Claim>();
    }
}
