using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Contract_Monthly_Claim_System.Services.Implementation
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserService> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // ===============================================
        // USER RETRIEVAL METHODS
        // ===============================================

        public async Task<User?> GetCurrentUserAsync(ClaimsPrincipal principal)
        {
            if (!principal.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("User is not authenticated");
                return null;
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value ??
                       principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email claim found for authenticated user");
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found in Users table", email);
                return null;
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                // Load specific type details if needed
                switch (user.UserType)
                {
                    case UserType.Lecturer:
                        return await _context.Lecturers
                            .Include(l => l.LecturerModules)
                            .FirstOrDefaultAsync(l => l.UserId == userId);
                    case UserType.ProgrammeCoordinator:
                        return await _context.ProgrammeCoordinators
                            .Include(pc => pc.ManagedProgrammes)
                            .FirstOrDefaultAsync(pc => pc.UserId == userId);
                    case UserType.AcademicManager:
                        return await _context.AcademicManagers
                            .FirstOrDefaultAsync(am => am.UserId == userId);
                }
            }

            return user;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserByIdentityIdAsync(string identityUserId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
        }

        public async Task<List<User>> GetUsersByTypeAsync(UserType userType)
        {
            return await _context.Users
                .Where(u => u.UserType == userType && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<List<Lecturer>> GetAllLecturersAsync()
        {
            return await _context.Lecturers
                .Include(l => l.LecturerModules)
                .Where(l => l.IsActive)
                .OrderBy(l => l.LastName)
                .ThenBy(l => l.FirstName)
                .ToListAsync();
        }

        public async Task<List<ProgrammeCoordinator>> GetAllCoordinatorsAsync()
        {
            return await _context.ProgrammeCoordinators
                .Include(pc => pc.ManagedProgrammes)
                .Where(pc => pc.IsActive)
                .OrderBy(pc => pc.LastName)
                .ThenBy(pc => pc.FirstName)
                .ToListAsync();
        }

        public async Task<List<AcademicManager>> GetAllManagersAsync()
        {
            return await _context.AcademicManagers
                .Where(am => am.IsActive)
                .OrderBy(am => am.LastName)
                .ThenBy(am => am.FirstName)
                .ToListAsync();
        }

        // ===============================================
        // USER SEARCH AND PAGINATION
        // ===============================================

        public async Task<PagedResultDto<User>> SearchUsersAsync(UserSearchDto searchDto)
        {
            var query = _context.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchDto.SearchTerm))
            {
                var searchTerm = searchDto.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
            }

            // Apply user type filter
            if (searchDto.UserType.HasValue)
            {
                query = query.Where(u => u.UserType == searchDto.UserType.Value);
            }

            // Apply active status filter
            if (searchDto.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == searchDto.IsActive.Value);
            }

            // Get total count
            var totalItems = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)searchDto.PageSize);

            return new PagedResultDto<User>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = searchDto.PageNumber,
                PageSize = searchDto.PageSize,
                TotalPages = totalPages,
                HasPreviousPage = searchDto.PageNumber > 1,
                HasNextPage = searchDto.PageNumber < totalPages
            };
        }

        // ===============================================
        // USER CREATION AND UPDATE
        // ===============================================

        public async Task<(bool Success, string Message, User? User)> CreateUserAsync(CreateUserViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return (false, "A user with this email already exists", null);
                }

                // Create Identity user
                var identityUser = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(identityUser, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return (false, $"Failed to create user account: {errors}", null);
                }

                // Add user to role
                await _userManager.AddToRoleAsync(identityUser, model.UserType.ToString());

                // Create application user based on type
                User appUser = model.UserType switch
                {
                    UserType.Lecturer => new Lecturer
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        UserType = UserType.Lecturer,
                        IdentityUserId = identityUser.Id,
                        EmployeeNumber = model.EmployeeNumber,
                        Specialization = model.Specialization,
                        DefaultHourlyRate = model.DefaultHourlyRate,
                        BankAccountNumber = model.BankAccountNumber,
                        TaxNumber = model.TaxNumber
                    },
                    UserType.ProgrammeCoordinator => new ProgrammeCoordinator
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        UserType = UserType.ProgrammeCoordinator,
                        IdentityUserId = identityUser.Id,
                        Department = model.Department
                    },
                    UserType.AcademicManager => new AcademicManager
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        UserType = UserType.AcademicManager,
                        IdentityUserId = identityUser.Id,
                        Division = model.Division,
                        ApprovalLimit = model.ApprovalLimit
                    },
                    _ => throw new ArgumentException("Invalid user type")
                };

                _context.Users.Add(appUser);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("User {Email} created successfully as {UserType}",
                    model.Email, model.UserType);

                return (true, "User created successfully", appUser);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user {Email}", model.Email);
                return (false, "An error occurred while creating the user", null);
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            try
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.IsActive = model.IsActive;

                // Update type-specific properties
                switch (user)
                {
                    case Lecturer lecturer:
                        lecturer.Specialization = model.Specialization;
                        lecturer.DefaultHourlyRate = model.DefaultHourlyRate;
                        lecturer.BankAccountNumber = model.BankAccountNumber;
                        break;
                    case ProgrammeCoordinator coordinator:
                        coordinator.Department = model.Department;
                        break;
                    case AcademicManager manager:
                        manager.Division = model.Division;
                        manager.ApprovalLimit = model.ApprovalLimit;
                        break;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} updated successfully", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deactivated", userId);
            return true;
        }

        public async Task<bool> ReactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} reactivated", userId);
            return true;
        }

        // ===============================================
        // ROLE MANAGEMENT
        // ===============================================

        public async Task<bool> AssignUserToRoleAsync(int userId, string role)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            if (!await _roleManager.RoleExistsAsync(role))
            {
                _logger.LogWarning("Role {Role} does not exist", role);
                return false;
            }

            var result = await _userManager.AddToRoleAsync(identityUser, role);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} assigned to role {Role}", userId, role);
            }

            return result.Succeeded;
        }

        public async Task<bool> RemoveUserFromRoleAsync(int userId, string role)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            var result = await _userManager.RemoveFromRoleAsync(identityUser, role);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} removed from role {Role}", userId, role);
            }

            return result.Succeeded;
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return new List<string>();

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return new List<string>();

            var roles = await _userManager.GetRolesAsync(identityUser);
            return roles.ToList();
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string role)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            return await _userManager.IsInRoleAsync(identityUser, role);
        }

        // ===============================================
        // USER STATISTICS
        // ===============================================

        public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return new UserStatistics();

            var stats = new UserStatistics
            {
                UserId = userId,
                FullName = user.FullName,
                UserType = user.UserType,
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate,
                AccountAge = (DateTime.UtcNow - user.CreatedDate).Days
            };

            if (user.UserType == UserType.Lecturer)
            {
                stats.TotalClaims = await _context.Claims
                    .CountAsync(c => c.LecturerId == userId);

                stats.ApprovedClaims = await _context.Claims
                    .CountAsync(c => c.LecturerId == userId &&
                        (c.Status == ClaimStatus.ManagerApproved || c.Status == ClaimStatus.Paid));

                stats.TotalEarnings = await _context.Claims
                    .Where(c => c.LecturerId == userId &&
                        (c.Status == ClaimStatus.ManagerApproved || c.Status == ClaimStatus.Paid))
                    .SumAsync(c => (decimal?)c.TotalAmount) ?? 0;

                stats.PendingClaims = await _context.Claims
                    .CountAsync(c => c.LecturerId == userId &&
                        (c.Status == ClaimStatus.Submitted ||
                         c.Status == ClaimStatus.UnderCoordinatorReview ||
                         c.Status == ClaimStatus.UnderManagerReview));

                stats.TotalHoursWorked = await _context.Claims
                    .Where(c => c.LecturerId == userId)
                    .SumAsync(c => (decimal?)c.TotalHours) ?? 0;
            }
            else if (user.UserType == UserType.ProgrammeCoordinator)
            {
                var programmeIds = await _context.Programmes
                    .Where(p => p.CoordinatorId == userId)
                    .Select(p => p.ProgrammeId)
                    .ToListAsync();

                stats.TotalClaimsReviewed = await _context.Claims
                    .CountAsync(c => c.CoordinatorId == userId);

                stats.ClaimsAwaitingAction = await _context.Claims
                    .CountAsync(c => c.Status == ClaimStatus.Submitted &&
                        c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)));

                stats.ManagedProgrammes = programmeIds.Count;
            }
            else if (user.UserType == UserType.AcademicManager)
            {
                stats.TotalClaimsReviewed = await _context.Claims
                    .CountAsync(c => c.ManagerId == userId);

                stats.ClaimsAwaitingAction = await _context.Claims
                    .CountAsync(c => c.Status == ClaimStatus.CoordinatorApproved);

                stats.TotalApprovedAmount = await _context.Claims
                    .Where(c => c.ManagerId == userId && c.Status == ClaimStatus.ManagerApproved)
                    .SumAsync(c => (decimal?)c.TotalAmount) ?? 0;
            }

            return stats;
        }

        public async Task<List<UserActivityLog>> GetUserActivityAsync(int userId, int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            var activities = new List<UserActivityLog>();

            // Get claim activities
            var claimActivities = await _context.ClaimStatusHistories
                .Include(csh => csh.Claim)
                .Where(csh => csh.ChangedByUserId == userId && csh.StatusChangeDate >= fromDate)
                .OrderByDescending(csh => csh.StatusChangeDate)
                .Take(50)
                .Select(csh => new UserActivityLog
                {
                    ActivityDate = csh.StatusChangeDate,
                    ActivityType = "Claim Status Change",
                    Description = $"Changed claim {csh.Claim.ClaimNumber} from {csh.PreviousStatus} to {csh.NewStatus}",
                    RelatedEntityId = csh.ClaimId,
                    RelatedEntityType = "Claim"
                })
                .ToListAsync();

            activities.AddRange(claimActivities);

            // Get document uploads
            var documentActivities = await _context.Documents
                .Include(d => d.Claim)
                .Where(d => d.UploadedByUserId == userId && d.UploadDate >= fromDate)
                .OrderByDescending(d => d.UploadDate)
                .Take(50)
                .Select(d => new UserActivityLog
                {
                    ActivityDate = d.UploadDate,
                    ActivityType = "Document Upload",
                    Description = $"Uploaded document {d.FileName} for claim {d.Claim.ClaimNumber}",
                    RelatedEntityId = d.DocumentId,
                    RelatedEntityType = "Document"
                })
                .ToListAsync();

            activities.AddRange(documentActivities);

            return activities.OrderByDescending(a => a.ActivityDate).Take(100).ToList();
        }

        // ===============================================
        // PASSWORD MANAGEMENT
        // ===============================================

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            var result = await _userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Password change failed for user {UserId}: {Errors}", userId, errors);
            }

            return result.Succeeded;
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await GetUserByIdAsync(userId);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
            var result = await _userManager.ResetPasswordAsync(identityUser, resetToken, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successfully for user {UserId}", userId);
            }

            return result.Succeeded;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user?.IdentityUserId == null)
                return string.Empty;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return string.Empty;

            return await _userManager.GeneratePasswordResetTokenAsync(identityUser);
        }

        // ===============================================
        // PROFILE MANAGEMENT
        // ===============================================

        public async Task<UserProfileViewModel?> GetUserProfileAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return null;

            var roles = await GetUserRolesAsync(userId);
            var stats = await GetUserStatisticsAsync(userId);

            var profile = new UserProfileViewModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate,
                Roles = roles,
                Statistics = stats
            };

            // Add type-specific information
            switch (user)
            {
                case Lecturer lecturer:
                    profile.EmployeeNumber = lecturer.EmployeeNumber;
                    profile.Specialization = lecturer.Specialization;
                    profile.DefaultHourlyRate = lecturer.DefaultHourlyRate;
                    profile.AssignedModules = await _context.LecturerModules
                        .Where(lm => lm.LecturerId == userId && lm.IsActive)
                        .Include(lm => lm.Module)
                        .Select(lm => lm.Module.ModuleName)
                        .ToListAsync();
                    break;

                case ProgrammeCoordinator coordinator:
                    profile.Department = coordinator.Department;
                    profile.ManagedProgrammes = await _context.Programmes
                        .Where(p => p.CoordinatorId == userId)
                        .Select(p => p.ProgrammeName)
                        .ToListAsync();
                    break;

                case AcademicManager manager:
                    profile.Division = manager.Division;
                    profile.ApprovalLimit = manager.ApprovalLimit;
                    break;
            }

            return profile;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UserProfileViewModel model)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            try
            {
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                await _context.SaveChangesAsync();
                _logger.LogInformation("User profile updated for {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for {UserId}", userId);
                return false;
            }
        }

        // ===============================================
        // VALIDATION METHODS
        // ===============================================

        public async Task<bool> ValidateUserCredentialsAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user?.IdentityUserId == null)
                return false;

            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            if (identityUser == null)
                return false;

            return await _userManager.CheckPasswordAsync(identityUser, password);
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateUserDataAsync(CreateUserViewModel model)
        {
            var errors = new List<string>();

            // Check email format
            if (!IsValidEmail(model.Email))
            {
                errors.Add("Invalid email format");
            }

            // Check if email exists
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                errors.Add("Email already exists");
            }

            // Validate phone number format
            if (!string.IsNullOrEmpty(model.PhoneNumber) && !IsValidPhoneNumber(model.PhoneNumber))
            {
                errors.Add("Invalid phone number format");
            }

            // Type-specific validation
            switch (model.UserType)
            {
                case UserType.Lecturer:
                    if (string.IsNullOrEmpty(model.EmployeeNumber))
                        errors.Add("Employee number is required for lecturers");
                    if (model.DefaultHourlyRate <= 0)
                        errors.Add("Default hourly rate must be greater than zero");
                    break;

                case UserType.ProgrammeCoordinator:
                    if (string.IsNullOrEmpty(model.Department))
                        errors.Add("Department is required for coordinators");
                    break;

                case UserType.AcademicManager:
                    if (string.IsNullOrEmpty(model.Division))
                        errors.Add("Division is required for managers");
                    if (model.ApprovalLimit <= 0)
                        errors.Add("Approval limit must be greater than zero");
                    break;
            }

            return (errors.Count == 0, errors);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Simple validation - adjust based on your requirements
            return System.Text.RegularExpressions.Regex.IsMatch(
                phoneNumber,
                @"^[\d\s\-\+\(\)]+$");
        }
    }
}
