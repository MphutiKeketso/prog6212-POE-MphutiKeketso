namespace Contract_Monthly_Claim_System.Models.View
{
    public class DocumentViewModel
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public string FileSizeDisplay { get; set; } = string.Empty;
        public string FileIcon { get; set; } = string.Empty;
    }

    public class DocumentMetadata
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string ClaimNumber { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool FileExists { get; set; }
    }

}
