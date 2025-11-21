namespace Contract_Monthly_Claim_System.Models.DTOs
{
    public class ClaimSearchDto
    {
        public string? SearchTerm { get; set; }
        public ClaimStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? LecturerId { get; set; }
        public int? ProgrammeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "SubmissionDate";
        public string SortDirection { get; set; } = "DESC";
    }

    public class UserSearchDto
    {
        public string? SearchTerm { get; set; }
        public UserType? UserType { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
