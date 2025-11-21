namespace Contract_Monthly_Claim_System.Models
{
    public class LecturerModule
    {
        public int LecturerId { get; set; }
        public int ModuleId { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Lecturer Lecturer { get; set; } = null!;
        public virtual Module Module { get; set; } = null!;
    }
}
