namespace Contract_Monthly_Claim_System.Models
{
    public abstract class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; } = true;
        public UserType UserType { get; set; }

        // Navigation property for ASP.NET Core Identity
        public string? IdentityUserId { get; set; }

        // Full name property for display
        public string FullName => $"{FirstName} {LastName}";
    }
}