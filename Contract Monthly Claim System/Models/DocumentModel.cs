namespace Contract_Monthly_Claim_System.Models
{
    public class Document
    {
        public int DocumentId { get; set; }
        public int ClaimId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int UploadedByUserId { get; set; }

        // Navigation properties
        public virtual Claim Claim { get; set; } = null!;
        public virtual User UploadedBy { get; set; } = null!;
    }
}
