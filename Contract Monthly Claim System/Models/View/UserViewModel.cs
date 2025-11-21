namespace Contract_Monthly_Claim_System.Models.View
{
    public class CreateUserViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserType UserType { get; set; }

        // Lecturer-specific
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public decimal DefaultHourlyRate { get; set; }
        public string BankAccountNumber { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;

        // Coordinator-specific
        public string Department { get; set; } = string.Empty;

        // Manager-specific
        public string Division { get; set; } = string.Empty;
        public decimal ApprovalLimit { get; set; }
    }

    public class UpdateUserViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Lecturer-specific
        public string Specialization { get; set; } = string.Empty;
        public decimal DefaultHourlyRate { get; set; }
        public string BankAccountNumber { get; set; } = string.Empty;

        // Coordinator-specific
        public string Department { get; set; } = string.Empty;

        // Manager-specific
        public string Division { get; set; } = string.Empty;
        public decimal ApprovalLimit { get; set; }
    }

    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<string> Roles { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();

        // Lecturer-specific
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public decimal DefaultHourlyRate { get; set; }
        public List<string> AssignedModules { get; set; } = new();

        // Coordinator-specific
        public string Department { get; set; } = string.Empty;
        public List<string> ManagedProgrammes { get; set; } = new();

        // Manager-specific
        public string Division { get; set; } = string.Empty;
        public decimal ApprovalLimit { get; set; }
    }

    public class UserStatistics
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int AccountAge { get; set; }

        // Lecturer statistics
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingClaims { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalHoursWorked { get; set; }

        // Coordinator/Manager statistics
        public int TotalClaimsReviewed { get; set; }
        public int ClaimsAwaitingAction { get; set; }
        public int ManagedProgrammes { get; set; }
        public decimal TotalApprovedAmount { get; set; }
    }

    public class UserActivityLog
    {
        public DateTime ActivityDate { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; } = string.Empty;
    }
}
