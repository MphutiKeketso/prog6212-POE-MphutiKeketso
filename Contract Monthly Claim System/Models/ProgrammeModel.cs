using System.Reflection;

namespace Contract_Monthly_Claim_System.Models
{
    public class Programme
    {
        public int ProgrammeId { get; set; }
        public string ProgrammeName { get; set; } = string.Empty;
        public string ProgrammeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CoordinatorId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ProgrammeCoordinator Coordinator { get; set; } = null!;
        public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    }
}
