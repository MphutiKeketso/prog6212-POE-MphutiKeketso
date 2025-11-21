using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Models
{
   
    public class Lecturer : User
    {
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public decimal DefaultHourlyRate { get; set; }
        public string BankAccountNumber { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public virtual ICollection<LecturerModule> LecturerModules { get; set; } = new List<LecturerModule>();
    }
}
