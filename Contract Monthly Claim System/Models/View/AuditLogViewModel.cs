namespace Contract_Monthly_Claim_System.Models.View
{
    public class AuditLogViewModel
    {
        public int AuditId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }
}
