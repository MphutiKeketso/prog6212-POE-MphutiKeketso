using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;
using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IUserService
    {
        // Core retrieval methods
        Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdentityIdAsync(string identityUserId);
        Task<List<User>> GetUsersByTypeAsync(UserType userType);
        Task<List<Lecturer>> GetAllLecturersAsync();
        Task<List<ProgrammeCoordinator>> GetAllCoordinatorsAsync();
        Task<List<AcademicManager>> GetAllManagersAsync();

        // Search and pagination
        Task<PagedResultDto<User>> SearchUsersAsync(UserSearchDto searchDto);

        // User creation and updates
        Task<(bool Success, string Message, User? User)> CreateUserAsync(CreateUserViewModel model);
        Task<bool> UpdateUserAsync(int userId, UpdateUserViewModel model);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ReactivateUserAsync(int userId);

        // Role management
        Task<bool> AssignUserToRoleAsync(int userId, string role);
        Task<bool> RemoveUserFromRoleAsync(int userId, string role);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> IsUserInRoleAsync(int userId, string role);

        // Statistics and activity
        Task<UserStatistics> GetUserStatisticsAsync(int userId);
        Task<List<UserActivityLog>> GetUserActivityAsync(int userId, int days = 30);

        // Password management
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<string> GeneratePasswordResetTokenAsync(string email);

        // Profile management
        Task<UserProfileViewModel?> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UserProfileViewModel model);

        // Validation
        Task<bool> ValidateUserCredentialsAsync(string email, string password);
        Task<(bool IsValid, List<string> Errors)> ValidateUserDataAsync(CreateUserViewModel model);
    }
}
