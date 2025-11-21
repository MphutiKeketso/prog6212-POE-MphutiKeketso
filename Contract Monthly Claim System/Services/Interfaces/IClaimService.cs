using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IClaimService
    {
        Task<PagedResultDto<ClaimDetailsViewModel>> SearchClaimsAsync(ClaimSearchDto searchDto, int userId);
        Task<ClaimDetailsViewModel?> GetClaimDetailsAsync(int claimId);
        Task<Claim?> GetClaimForEditAsync(int claimId);
        Task<int> CreateClaimAsync(CreateClaimViewModel model, int lecturerId);
        Task UpdateClaimAsync(int claimId, CreateClaimViewModel model);
        Task ProcessApprovalAsync(ApprovalViewModel model, int approverId);
        Task<DashboardStatsViewModel> GetDashboardStatsAsync(int userId);
        Task<List<RecentClaimViewModel>> GetRecentClaimsAsync(int userId, int count);
        Task<List<PendingApprovalViewModel>> GetPendingApprovalsAsync(int userId);
        Task<ClaimStatus> GetClaimStatusAsync(int claimId);
        Task<byte[]> GenerateClaimPdfAsync(int claimId);
        // Add these lines to your IClaimService.cs file

        Task<EnhancedClaimDetailsViewModel> GetEnhancedClaimDetailsAsync(int claimId, int userId);

        Task<CoordinatorDashboardViewModel> GetCoordinatorDashboardAsync(int coordinatorId);

        Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int managerId);
    }
}
