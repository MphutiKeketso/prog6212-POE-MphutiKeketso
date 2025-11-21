using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationViewModel>> GetRecentNotificationsAsync(int userId, int count);
        Task<int> GetUnreadNotificationCountAsync(int userId);
        Task SendClaimStatusNotificationAsync(int claimId, ClaimStatus newStatus, string comments = "");
        Task SendEmailNotificationAsync(string toEmail, string subject, string body);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task NotifyClaimSubmittedAsync(int claimId);
    }
}
