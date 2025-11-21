namespace Contract_Monthly_Claim_System.Models
{
    public enum UserType
    {
        Lecturer = 1,
        ProgrammeCoordinator = 2,
        AcademicManager = 3,
        SystemAdministrator = 4
    }

    public enum ClaimStatus
    {
        Draft = 1,
        Submitted = 2,
        UnderCoordinatorReview = 3,
        CoordinatorApproved = 4,
        CoordinatorRejected = 5,
        UnderManagerReview = 6,
        ManagerApproved = 7,
        ManagerRejected = 8,
        Paid = 9,
        Cancelled = 10
    }

    public enum DocumentType
    {
        Timesheet = 1,
        LectureSchedule = 2,
        AttendanceRegister = 3,
        ContractDocument = 4,
        SupportingEvidence = 5,
        Other = 99
    }
}
