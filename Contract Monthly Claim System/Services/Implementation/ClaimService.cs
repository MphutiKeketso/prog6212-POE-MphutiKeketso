using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.DTOs;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ClaimService : IClaimService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClaimService> _logger;
    private readonly INotificationService _notificationService;

    public ClaimService(
        ApplicationDbContext context,
        ILogger<ClaimService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    // ===============================================
    // SEARCH AND RETRIEVAL METHODS
    // ===============================================

    public async Task<PagedResultDto<ClaimDetailsViewModel>> SearchClaimsAsync(ClaimSearchDto searchDto, int userId)
    {
        var query = _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.ClaimItems)
            .AsQueryable();

        // Filter by user permissions
        var currentUser = await _context.Users.FindAsync(userId);
        if (currentUser?.UserType == UserType.Lecturer)
        {
            query = query.Where(c => c.LecturerId == userId);
        }
        else if (currentUser?.UserType == UserType.ProgrammeCoordinator)
        {
            // Coordinators see claims for their programmes
            var coordinatorProgrammes = await _context.Programmes
                .Where(p => p.CoordinatorId == userId)
                .Select(p => p.ProgrammeId)
                .ToListAsync();

            query = query.Where(c => c.ClaimItems.Any(ci => coordinatorProgrammes.Contains(ci.Module.ProgrammeId)));
        }
        // Academic Managers see all claims

        // Apply search filters
        if (!string.IsNullOrEmpty(searchDto.SearchTerm))
        {
            query = query.Where(c => c.ClaimNumber.Contains(searchDto.SearchTerm) ||
                                     c.Lecturer.FirstName.Contains(searchDto.SearchTerm) ||
                                     c.Lecturer.LastName.Contains(searchDto.SearchTerm));
        }

        if (searchDto.Status.HasValue)
        {
            query = query.Where(c => c.Status == searchDto.Status.Value);
        }

        if (searchDto.FromDate.HasValue)
        {
            query = query.Where(c => c.SubmissionDate >= searchDto.FromDate.Value);
        }

        if (searchDto.ToDate.HasValue)
        {
            query = query.Where(c => c.SubmissionDate <= searchDto.ToDate.Value);
        }

        if (searchDto.LecturerId.HasValue)
        {
            query = query.Where(c => c.LecturerId == searchDto.LecturerId.Value);
        }

        // Apply sorting
        query = searchDto.SortBy?.ToLower() switch
        {
            "claimnumber" => searchDto.SortDirection == "DESC"
                ? query.OrderByDescending(c => c.ClaimNumber)
                : query.OrderBy(c => c.ClaimNumber),
            "claimmonth" => searchDto.SortDirection == "DESC"
                ? query.OrderByDescending(c => c.ClaimMonth)
                : query.OrderBy(c => c.ClaimMonth),
            "totalamount" => searchDto.SortDirection == "DESC"
                ? query.OrderByDescending(c => c.TotalAmount)
                : query.OrderBy(c => c.TotalAmount),
            "status" => searchDto.SortDirection == "DESC"
                ? query.OrderByDescending(c => c.Status)
                : query.OrderBy(c => c.Status),
            _ => query.OrderByDescending(c => c.SubmissionDate)
        };

        // Get total count
        var totalItems = await query.CountAsync();

        // Apply pagination
        var items = await query
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(c => new ClaimDetailsViewModel
            {
                ClaimId = c.ClaimId,
                ClaimNumber = c.ClaimNumber,
                LecturerName = $"{c.Lecturer.FirstName} {c.Lecturer.LastName}",
                ClaimMonth = c.ClaimMonth,
                SubmissionDate = c.SubmissionDate,
                TotalHours = c.TotalHours,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                StatusDisplay = c.Status.ToString().Replace("_", " "),
                CanEdit = c.Status == ClaimStatus.Draft || c.Status == ClaimStatus.CoordinatorRejected,
                CanApprove = (currentUser.UserType == UserType.ProgrammeCoordinator &&
                              c.Status == ClaimStatus.Submitted) ||
                             (currentUser.UserType == UserType.AcademicManager &&
                              c.Status == ClaimStatus.CoordinatorApproved)
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalItems / (double)searchDto.PageSize);

        return new PagedResultDto<ClaimDetailsViewModel>
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

    public async Task<ClaimDetailsViewModel?> GetClaimDetailsAsync(int claimId)
    {
        var claim = await _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.ClaimItems)
                .ThenInclude(ci => ci.Module)
                    .ThenInclude(m => m.Programme)
            .Include(c => c.Documents)
            .Include(c => c.StatusHistory)
                .ThenInclude(sh => sh.ChangedBy)
            .Include(c => c.Coordinator)
            .Include(c => c.Manager)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null) return null;

        return new ClaimDetailsViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            LecturerName = $"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}",
            ClaimMonth = claim.ClaimMonth,
            SubmissionDate = claim.SubmissionDate,
            TotalHours = claim.TotalHours,
            TotalAmount = claim.TotalAmount,
            Status = claim.Status,
            StatusDisplay = claim.Status.ToString().Replace("_", " "),
            Notes = claim.Notes,
            ClaimItems = claim.ClaimItems.Select(ci => new ClaimItemViewModel
            {
                ModuleId = ci.ModuleId,
                ModuleName = ci.Module.ModuleName,
                ModuleCode = ci.Module.ModuleCode,
                HoursWorked = ci.HoursWorked,
                HourlyRate = ci.HourlyRate,
                TotalAmount = ci.TotalAmount,
                Description = ci.Description,
                WorkDate = ci.WorkDate
            }).ToList(),
            Documents = claim.Documents.Select(d => new DocumentViewModel
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadDate = d.UploadDate,
                Description = d.Description,
                FileSizeDisplay = FormatFileSize(d.FileSize),
                FileIcon = GetFileIcon(d.ContentType)
            }).ToList(),
            StatusHistory = claim.StatusHistory
                .OrderByDescending(sh => sh.StatusChangeDate)
                .Select(sh => new StatusHistoryViewModel
                {
                    PreviousStatus = sh.PreviousStatus,
                    NewStatus = sh.NewStatus,
                    StatusChangeDate = sh.StatusChangeDate,
                    ChangedByName = $"{sh.ChangedBy.FirstName} {sh.ChangedBy.LastName}",
                    Comments = sh.Comments,
                    StatusChangeDisplay = $"{sh.PreviousStatus.ToString().Replace("_", " ")} → {sh.NewStatus.ToString().Replace("_", " ")}"
                }).ToList(),
            CoordinatorApproval = claim.CoordinatorApprovalDate.HasValue ? new ApprovalDetailsViewModel
            {
                ApprovalDate = claim.CoordinatorApprovalDate.Value,
                ApproverName = claim.Coordinator != null ? $"{claim.Coordinator.FirstName} {claim.Coordinator.LastName}" : "Unknown",
                Comments = claim.CoordinatorNotes
            } : null,
            ManagerApproval = claim.ManagerApprovalDate.HasValue ? new ApprovalDetailsViewModel
            {
                ApprovalDate = claim.ManagerApprovalDate.Value,
                ApproverName = claim.Manager != null ? $"{claim.Manager.FirstName} {claim.Manager.LastName}" : "Unknown",
                Comments = claim.ManagerNotes
            } : null,
            CanEdit = claim.Status == ClaimStatus.Draft || claim.Status == ClaimStatus.CoordinatorRejected,
            CanApprove = false, // Will be set based on current user context
            CanReject = false    // Will be set based on current user context
        };
    }

    public async Task<Claim?> GetClaimForEditAsync(int claimId)
    {
        return await _context.Claims
            .Include(c => c.ClaimItems)
                .ThenInclude(ci => ci.Module)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);
    }

    public async Task<ClaimStatus> GetClaimStatusAsync(int claimId)
    {
        var claim = await _context.Claims.FindAsync(claimId);
        return claim?.Status ?? ClaimStatus.Draft;
    }

    // ===============================================
    // CREATE AND UPDATE METHODS
    // ===============================================

    public async Task<int> CreateClaimAsync(CreateClaimViewModel model, int lecturerId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate lecturer exists
            var lecturer = await _context.Lecturers.FindAsync(lecturerId);
            if (lecturer == null)
                throw new ArgumentException("Lecturer not found");

            // Check for duplicate claim in same month
            var existingClaim = await _context.Claims
                .AnyAsync(c => c.LecturerId == lecturerId &&
                               c.ClaimMonth.Year == model.ClaimMonth.Year &&
                               c.ClaimMonth.Month == model.ClaimMonth.Month &&
                               c.Status != ClaimStatus.Cancelled);

            if (existingClaim)
                throw new InvalidOperationException("A claim already exists for this month");

            // Generate claim number
            var claimNumber = await GenerateClaimNumberAsync();

            // Create claim
            var claim = new Claim
            {
                LecturerId = lecturerId,
                ClaimNumber = claimNumber,
                ClaimMonth = model.ClaimMonth,
                SubmissionDate = model.SubmissionDate,
                Notes = model.Notes ?? string.Empty,
                Status = ClaimStatus.Submitted,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Add claim items
            foreach (var itemModel in model.ClaimItems)
            {
                // Validate module assignment
                var isAssigned = await _context.LecturerModules
                    .AnyAsync(lm => lm.LecturerId == lecturerId &&
                                   lm.ModuleId == itemModel.ModuleId &&
                                   lm.IsActive);

                if (!isAssigned)
                    throw new UnauthorizedAccessException($"Lecturer not assigned to module {itemModel.ModuleCode}");

                var claimItem = new ClaimItem
                {
                    ClaimId = claim.ClaimId,
                    ModuleId = itemModel.ModuleId,
                    HoursWorked = itemModel.HoursWorked,
                    HourlyRate = itemModel.HourlyRate,
                    TotalAmount = itemModel.HoursWorked * itemModel.HourlyRate,
                    Description = itemModel.Description ?? string.Empty,
                    WorkDate = itemModel.WorkDate,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ClaimItems.Add(claimItem);
            }

            await _context.SaveChangesAsync();

            // Update claim totals
            claim.TotalHours = claim.ClaimItems.Sum(ci => ci.HoursWorked);
            claim.TotalAmount = claim.ClaimItems.Sum(ci => ci.TotalAmount);

            // Add status history
            var statusHistory = new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                PreviousStatus = ClaimStatus.Draft,
                NewStatus = ClaimStatus.Submitted,
                StatusChangeDate = DateTime.UtcNow,
                ChangedByUserId = lecturerId,
                Comments = "Claim submitted for approval",
                SystemNotes = $"Created with {claim.ClaimItems.Count} items totaling {claim.TotalAmount:C}"
            };

            _context.ClaimStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Send notification
            await _notificationService.SendClaimStatusNotificationAsync(claim.ClaimId, ClaimStatus.Submitted);

            _logger.LogInformation("Claim {ClaimNumber} created successfully by lecturer {LecturerId}", claimNumber, lecturerId);

            return claim.ClaimId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating claim for lecturer {LecturerId}", lecturerId);
            throw;
        }
    }

    public async Task UpdateClaimAsync(int claimId, CreateClaimViewModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var claim = await _context.Claims
                .Include(c => c.ClaimItems)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
                throw new ArgumentException("Claim not found");

            // Verify claim can be edited
            if (claim.Status != ClaimStatus.Draft && claim.Status != ClaimStatus.CoordinatorRejected)
                throw new InvalidOperationException("Claim cannot be edited in its current status");

            // Update claim properties
            claim.ClaimMonth = model.ClaimMonth;
            claim.SubmissionDate = model.SubmissionDate;
            claim.Notes = model.Notes ?? string.Empty;
            claim.Status = ClaimStatus.Submitted;
            claim.LastModifiedDate = DateTime.UtcNow;

            // Remove existing claim items
            _context.ClaimItems.RemoveRange(claim.ClaimItems);

            // Add updated claim items
            foreach (var itemModel in model.ClaimItems)
            {
                var claimItem = new ClaimItem
                {
                    ClaimId = claim.ClaimId,
                    ModuleId = itemModel.ModuleId,
                    HoursWorked = itemModel.HoursWorked,
                    HourlyRate = itemModel.HourlyRate,
                    TotalAmount = itemModel.HoursWorked * itemModel.HourlyRate,
                    Description = itemModel.Description ?? string.Empty,
                    WorkDate = itemModel.WorkDate,
                    CreatedDate = DateTime.UtcNow
                };

                _context.ClaimItems.Add(claimItem);
            }

            await _context.SaveChangesAsync();

            // Update claim totals
            claim.TotalHours = claim.ClaimItems.Sum(ci => ci.HoursWorked);
            claim.TotalAmount = claim.ClaimItems.Sum(ci => ci.TotalAmount);

            // Add status history
            var statusHistory = new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                PreviousStatus = claim.Status,
                NewStatus = ClaimStatus.Submitted,
                StatusChangeDate = DateTime.UtcNow,
                ChangedByUserId = claim.LecturerId,
                Comments = "Claim updated and resubmitted",
                SystemNotes = $"Updated with {claim.ClaimItems.Count} items totaling {claim.TotalAmount:C}"
            };

            _context.ClaimStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Claim {ClaimId} updated successfully", claimId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating claim {ClaimId}", claimId);
            throw;
        }
    }

    // ===============================================
    // APPROVAL METHODS
    // ===============================================

    public async Task ProcessApprovalAsync(ApprovalViewModel model, int approverId)
    {
        var claim = await _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.StatusHistory)
            .FirstOrDefaultAsync(c => c.ClaimId == model.ClaimId);

        if (claim == null)
            throw new ArgumentException("Claim not found");

        var approver = await _context.Users.FindAsync(approverId);
        if (approver == null)
            throw new ArgumentException("Approver not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var previousStatus = claim.Status;
            ClaimStatus newStatus;

            // Process based on user type and action
            if (model.Action.ToLower() == "approve")
            {
                if (approver.UserType == UserType.ProgrammeCoordinator)
                {
                    if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderCoordinatorReview)
                        throw new InvalidOperationException("Claim is not in a valid state for coordinator approval");

                    claim.CoordinatorId = approverId;
                    claim.CoordinatorApprovalDate = DateTime.UtcNow;
                    claim.CoordinatorNotes = model.Comments;
                    newStatus = ClaimStatus.CoordinatorApproved;
                }
                else if (approver.UserType == UserType.AcademicManager)
                {
                    if (claim.Status != ClaimStatus.CoordinatorApproved && claim.Status != ClaimStatus.UnderManagerReview)
                        throw new InvalidOperationException("Claim is not in a valid state for manager approval");

                    claim.ManagerId = approverId;
                    claim.ManagerApprovalDate = DateTime.UtcNow;
                    claim.ManagerNotes = model.Comments;
                    newStatus = ClaimStatus.ManagerApproved;
                }
                else
                {
                    throw new UnauthorizedAccessException("User not authorized to approve claims");
                }
            }
            else if (model.Action.ToLower() == "reject")
            {
                if (approver.UserType == UserType.ProgrammeCoordinator)
                {
                    if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderCoordinatorReview)
                        throw new InvalidOperationException("Claim is not in a valid state for coordinator rejection");

                    claim.CoordinatorId = approverId;
                    claim.CoordinatorApprovalDate = DateTime.UtcNow;
                    claim.CoordinatorNotes = model.Comments;
                    newStatus = ClaimStatus.CoordinatorRejected;
                }
                else if (approver.UserType == UserType.AcademicManager)
                {
                    if (claim.Status != ClaimStatus.CoordinatorApproved && claim.Status != ClaimStatus.UnderManagerReview)
                        throw new InvalidOperationException("Claim is not in a valid state for manager rejection");

                    claim.ManagerId = approverId;
                    claim.ManagerApprovalDate = DateTime.UtcNow;
                    claim.ManagerNotes = model.Comments;
                    newStatus = ClaimStatus.ManagerRejected;
                }
                else
                {
                    throw new UnauthorizedAccessException("User not authorized to reject claims");
                }
            }
            else
            {
                throw new ArgumentException("Invalid action. Must be 'approve' or 'reject'");
            }

            claim.Status = newStatus;
            claim.LastModifiedDate = DateTime.UtcNow;

            // Add status history
            var statusHistory = new ClaimStatusHistory
            {
                ClaimId = claim.ClaimId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                StatusChangeDate = DateTime.UtcNow,
                ChangedByUserId = approverId,
                Comments = model.Comments,
                SystemNotes = $"Processed by {approver.UserType}: {model.Action}"
            };

            _context.ClaimStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send notifications
            if (model.NotifyLecturer)
            {
                await _notificationService.SendClaimStatusNotificationAsync(claim.ClaimId, newStatus, model.Comments);
            }

            _logger.LogInformation("Claim {ClaimId} {Action} by {ApproverType} {ApproverId}",
                claim.ClaimId, model.Action, approver.UserType, approverId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing approval for claim {ClaimId}", model.ClaimId);
            throw;
        }
    }

    // ===============================================
    // DASHBOARD AND STATISTICS METHODS
    // ===============================================

    public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) throw new ArgumentException("User not found");

        IQueryable<Claim> query = _context.Claims;

        // Filter by user type
        if (user.UserType == UserType.Lecturer)
        {
            query = query.Where(c => c.LecturerId == userId);
        }
        else if (user.UserType == UserType.ProgrammeCoordinator)
        {
            var programmeIds = await _context.Programmes
                .Where(p => p.CoordinatorId == userId)
                .Select(p => p.ProgrammeId)
                .ToListAsync();

            query = query.Where(c => c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)));
        }

        var stats = new DashboardStatsViewModel
        {
            TotalClaims = await query.CountAsync(),
            PendingClaims = await query.CountAsync(c =>
                c.Status == ClaimStatus.Submitted ||
                c.Status == ClaimStatus.UnderCoordinatorReview ||
                c.Status == ClaimStatus.UnderManagerReview ||
                c.Status == ClaimStatus.CoordinatorApproved),
            ApprovedClaims = await query.CountAsync(c =>
                c.Status == ClaimStatus.ManagerApproved ||
                c.Status == ClaimStatus.Paid),
            RejectedClaims = await query.CountAsync(c =>
                c.Status == ClaimStatus.CoordinatorRejected ||
                c.Status == ClaimStatus.ManagerRejected),
            TotalEarned = await query
                .Where(c => c.Status == ClaimStatus.ManagerApproved || c.Status == ClaimStatus.Paid)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0
        };

        if (stats.ApprovedClaims > 0)
        {
            stats.AverageClaimAmount = stats.TotalEarned / stats.ApprovedClaims;
        }

        return stats;
    }

    public async Task<List<RecentClaimViewModel>> GetRecentClaimsAsync(int userId, int count)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new List<RecentClaimViewModel>();

        IQueryable<Claim> query = _context.Claims.Include(c => c.Lecturer);

        if (user.UserType == UserType.Lecturer)
        {
            query = query.Where(c => c.LecturerId == userId);
        }

        return await query
            .OrderByDescending(c => c.SubmissionDate)
            .Take(count)
            .Select(c => new RecentClaimViewModel
            {
                ClaimId = c.ClaimId,
                ClaimNumber = c.ClaimNumber,
                ClaimMonth = c.ClaimMonth,
                TotalAmount = c.TotalAmount,
                Status = c.Status,
                StatusDisplay = c.Status.ToString().Replace("_", " "),
                StatusClass = GetStatusCssClass(c.Status)
            })
            .ToListAsync();
    }

    public async Task<List<PendingApprovalViewModel>> GetPendingApprovalsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return new List<PendingApprovalViewModel>();

        IQueryable<Claim> query = _context.Claims
            .Include(c => c.Lecturer);

        // Filter based on user role
        if (user.UserType == UserType.ProgrammeCoordinator)
        {
            var programmeIds = await _context.Programmes
                .Where(p => p.CoordinatorId == userId)
                .Select(p => p.ProgrammeId)
                .ToListAsync();

            query = query.Where(c =>
                (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderCoordinatorReview) &&
                c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)));
        }
        else if (user.UserType == UserType.AcademicManager)
        {
            query = query.Where(c =>
                c.Status == ClaimStatus.CoordinatorApproved ||
                c.Status == ClaimStatus.UnderManagerReview);
        }
        else
        {
            return new List<PendingApprovalViewModel>();
        }

        return await query
            .OrderBy(c => c.SubmissionDate)
            .Select(c => new PendingApprovalViewModel
            {
                ClaimId = c.ClaimId,
                ClaimNumber = c.ClaimNumber,
                LecturerName = $"{c.Lecturer.FirstName} {c.Lecturer.LastName}",
                SubmissionDate = c.SubmissionDate,
                TotalAmount = c.TotalAmount
            })
            .ToListAsync();
    }

    // ===============================================
    // PDF GENERATION METHOD
    // ===============================================

    public async Task<byte[]> GenerateClaimPdfAsync(int claimId)
    {
        var claim = await GetClaimDetailsAsync(claimId);
        if (claim == null)
            throw new ArgumentException("Claim not found");

        // This is a placeholder - in production, you would use a library like:
        // - iTextSharp/iText7
        // - PdfSharp
        // - DinkToPdf (using wkhtmltopdf)
        // - QuestPDF

        _logger.LogInformation("PDF generation requested for claim {ClaimId}", claimId);

        // Return empty array as placeholder
        return Array.Empty<byte>();
    }

    // ===============================================
    // PRIVATE HELPER METHODS
    // ===============================================

    private async Task<string> GenerateClaimNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        var lastClaim = await _context.Claims
            .Where(c => c.ClaimNumber.StartsWith($"CLM-{year}-"))
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastClaim != null)
        {
            var parts = lastClaim.ClaimNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"CLM-{year}-{nextNumber:D4}";
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 Bytes";

        string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
        int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
        return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
    }

    private static string GetFileIcon(string contentType)
    {
        return contentType.ToLower() switch
        {
            "application/pdf" => "fas fa-file-pdf text-danger",
            "application/msword" or
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => "fas fa-file-word text-primary",
            "application/vnd.ms-excel" or
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => "fas fa-file-excel text-success",
            _ => "fas fa-file text-secondary"
        };
    }

    private static string GetStatusCssClass(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.Draft => "status-draft",
            ClaimStatus.Submitted => "status-submitted",
            ClaimStatus.UnderCoordinatorReview => "status-undercoordinatorreview",
            ClaimStatus.CoordinatorApproved => "status-coordinatorapproved",
            ClaimStatus.CoordinatorRejected => "status-coordinatorrejected",
            ClaimStatus.UnderManagerReview => "status-undermanagerreview",
            ClaimStatus.ManagerApproved => "status-managerapproved",
            ClaimStatus.ManagerRejected => "status-managerrejected",
            ClaimStatus.Paid => "status-paid",
            ClaimStatus.Cancelled => "status-cancelled",
            _ => "status-pending"
        };
    }

    // ==================================================================
    // == START: MERGED METHODS FROM SAMPLE 2
    // ==================================================================

    public async Task<EnhancedClaimDetailsViewModel> GetEnhancedClaimDetailsAsync(int claimId, int userId)
    {
        var claim = await _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.ClaimItems)
                .ThenInclude(ci => ci.Module)
                    .ThenInclude(m => m.Programme)
            .Include(c => c.Documents)
            .Include(c => c.StatusHistory)
                .ThenInclude(sh => sh.ChangedBy)
            .Include(c => c.Coordinator)
            .Include(c => c.Manager)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId);

        if (claim == null) return null;

        var viewModel = new EnhancedClaimDetailsViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            LecturerName = $"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}",
            ClaimMonth = claim.ClaimMonth,
            SubmissionDate = claim.SubmissionDate,
            TotalHours = claim.TotalHours,
            TotalAmount = claim.TotalAmount,
            Status = claim.Status,
            StatusDisplay = claim.Status.ToString().Replace("_", " "),
            Notes = claim.Notes,
            ClaimItems = claim.ClaimItems.Select(ci => new ClaimItemViewModel
            {
                ModuleId = ci.ModuleId,
                ModuleName = ci.Module.ModuleName,
                ModuleCode = ci.Module.ModuleCode,
                HoursWorked = ci.HoursWorked,
                HourlyRate = ci.HourlyRate,
                TotalAmount = ci.TotalAmount,
                Description = ci.Description,
                WorkDate = ci.WorkDate
            }).ToList(),
            Documents = claim.Documents.Select(d => new DocumentViewModel
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadDate = d.UploadDate,
                Description = d.Description,
                FileSizeDisplay = FormatFileSize(d.FileSize),
                FileIcon = GetFileIcon(d.ContentType)
            }).ToList(),

            // Progress tracking
            ProgressPercentage = CalculateProgress(claim.Status),
            CurrentStage = GetCurrentStage(claim.Status),
            ProgressSteps = BuildProgressSteps(claim),

            // Approval information
            CoordinatorApprovalInfo = claim.Coordinator != null ? new ApprovalInfo
            {
                ApproverName = $"{claim.Coordinator.FirstName} {claim.Coordinator.LastName}",
                ApproverRole = "Programme Coordinator",
                ApprovalDate = claim.CoordinatorApprovalDate,
                Decision = claim.Status == ClaimStatus.CoordinatorApproved ? "Approved" :
                           claim.Status == ClaimStatus.CoordinatorRejected ? "Rejected" : "Pending",
                Comments = claim.CoordinatorNotes,
                ApproverEmail = claim.Coordinator.Email
            } : null,

            ManagerApprovalInfo = claim.Manager != null ? new ApprovalInfo
            {
                ApproverName = $"{claim.Manager.FirstName} {claim.Manager.LastName}",
                ApproverRole = "Academic Manager",
                ApprovalDate = claim.ManagerApprovalDate,
                Decision = claim.Status == ClaimStatus.ManagerApproved ? "Approved" :
                           claim.Status == ClaimStatus.ManagerRejected ? "Rejected" : "Pending",
                Comments = claim.ManagerNotes,
                ApproverEmail = claim.Manager.Email
            } : null,

            // Timeline
            Timeline = BuildTimeline(claim)
        };

        return viewModel;
    }

    private int CalculateProgress(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.Draft => 0,
            ClaimStatus.Submitted => 25,
            ClaimStatus.UnderCoordinatorReview => 40,
            ClaimStatus.CoordinatorApproved => 60,
            ClaimStatus.UnderManagerReview => 75,
            ClaimStatus.ManagerApproved => 90,
            ClaimStatus.Paid => 100,
            ClaimStatus.CoordinatorRejected or ClaimStatus.ManagerRejected => 0,
            _ => 0
        };
    }

    private string GetCurrentStage(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.Draft => "Draft",
            ClaimStatus.Submitted => "Awaiting Coordinator Review",
            ClaimStatus.UnderCoordinatorReview => "Under Coordinator Review",
            ClaimStatus.CoordinatorApproved => "Awaiting Manager Review",
            ClaimStatus.UnderManagerReview => "Under Manager Review",
            ClaimStatus.ManagerApproved => "Approved - Awaiting Payment",
            ClaimStatus.Paid => "Paid",
            ClaimStatus.CoordinatorRejected => "Rejected by Coordinator",
            ClaimStatus.ManagerRejected => "Rejected by Manager",
            _ => "Unknown"
        };
    }

    private List<ClaimProgressStep> BuildProgressSteps(Claim claim)
    {
        var steps = new List<ClaimProgressStep>
        {
            new ClaimProgressStep
            {
                StepName = "Submitted",
                IsCompleted = claim.Status != ClaimStatus.Draft,
                IsCurrent = claim.Status == ClaimStatus.Submitted,
                CompletedDate = claim.SubmissionDate,
                Icon = "fas fa-paper-plane"
            },
            new ClaimProgressStep
            {
                StepName = "Coordinator Review",
                IsCompleted = claim.Status >= ClaimStatus.CoordinatorApproved,
                IsCurrent = claim.Status == ClaimStatus.UnderCoordinatorReview,
                CompletedDate = claim.CoordinatorApprovalDate,
                Icon = "fas fa-user-tie"
            },
            new ClaimProgressStep
            {
                StepName = "Manager Review",
                IsCompleted = claim.Status >= ClaimStatus.ManagerApproved,
                IsCurrent = claim.Status == ClaimStatus.UnderManagerReview,
                CompletedDate = claim.ManagerApprovalDate,
                Icon = "fas fa-user-graduate"
            },
            new ClaimProgressStep
            {
                StepName = "Paid",
                IsCompleted = claim.Status == ClaimStatus.Paid,
                IsCurrent = claim.Status == ClaimStatus.ManagerApproved,
                CompletedDate = claim.Status == ClaimStatus.Paid ? claim.LastModifiedDate : null,
                Icon = "fas fa-dollar-sign"
            }
        };

        return steps;
    }

    private List<StatusTimelineItem> BuildTimeline(Claim claim)
    {
        return claim.StatusHistory
            .OrderByDescending(sh => sh.StatusChangeDate)
            .Select(sh => new StatusTimelineItem
            {
                Date = sh.StatusChangeDate,
                Status = sh.NewStatus.ToString().Replace("_", " "),
                Description = sh.Comments,
                PerformedBy = $"{sh.ChangedBy.FirstName} {sh.ChangedBy.LastName}",
                Icon = GetStatusIcon(sh.NewStatus),
                ColorClass = GetStatusColor(sh.NewStatus)
            })
            .ToList();
    }

    private string GetStatusIcon(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.Submitted => "fas fa-paper-plane",
            ClaimStatus.CoordinatorApproved or ClaimStatus.ManagerApproved => "fas fa-check-circle",
            ClaimStatus.CoordinatorRejected or ClaimStatus.ManagerRejected => "fas fa-times-circle",
            ClaimStatus.Paid => "fas fa-dollar-sign",
            _ => "fas fa-circle"
        };
    }

    private string GetStatusColor(ClaimStatus status)
    {
        return status switch
        {
            ClaimStatus.CoordinatorApproved or ClaimStatus.ManagerApproved or ClaimStatus.Paid => "success",
            ClaimStatus.CoordinatorRejected or ClaimStatus.ManagerRejected => "danger",
            ClaimStatus.Submitted => "info",
            _ => "secondary"
        };
    }

    public async Task<CoordinatorDashboardViewModel> GetCoordinatorDashboardAsync(int coordinatorId)
    {
        var coordinator = await _context.ProgrammeCoordinators
            .Include(pc => pc.ManagedProgrammes)
            .FirstOrDefaultAsync(pc => pc.UserId == coordinatorId);

        if (coordinator == null)
            return new CoordinatorDashboardViewModel();

        var programmeIds = coordinator.ManagedProgrammes.Select(p => p.ProgrammeId).ToList();

        var pendingClaims = await _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.ClaimItems)
                .ThenInclude(ci => ci.Module)
                    .ThenInclude(m => m.Programme)
            .Include(c => c.Documents)
            .Where(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderCoordinatorReview) &&
                          c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)))
            .OrderBy(c => c.SubmissionDate)
            .ToListAsync();

        var viewModel = new CoordinatorDashboardViewModel
        {
            CoordinatorName = $"{coordinator.FirstName} {coordinator.LastName}",
            ManagedProgrammes = coordinator.ManagedProgrammes.Select(p => p.ProgrammeName).ToList(),
            PendingClaims = pendingClaims.Select(c => MapToPendingClaimViewModel(c)).ToList(),
            Statistics = await CalculateCoordinatorStatistics(coordinatorId, programmeIds)
        };

        return viewModel;
    }

    public async Task<ManagerDashboardViewModel> GetManagerDashboardAsync(int managerId)
    {
        var manager = await _context.AcademicManagers
            .FirstOrDefaultAsync(am => am.UserId == managerId);

        if (manager == null)
            return new ManagerDashboardViewModel();

        var pendingClaims = await _context.Claims
            .Include(c => c.Lecturer)
            .Include(c => c.ClaimItems)
                .ThenInclude(ci => ci.Module)
                    .ThenInclude(m => m.Programme)
            .Include(c => c.Documents)
            .Include(c => c.Coordinator)
            .Where(c => c.Status == ClaimStatus.CoordinatorApproved || c.Status == ClaimStatus.UnderManagerReview)
            .OrderBy(c => c.CoordinatorApprovalDate)
            .ToListAsync();

        var viewModel = new ManagerDashboardViewModel
        {
            ManagerName = $"{manager.FirstName} {manager.LastName}",
            PendingClaims = pendingClaims.Select(c => MapToPendingClaimViewModel(c)).ToList(),
            Statistics = await CalculateManagerStatistics(managerId)
        };

        return viewModel;
    }

    private PendingClaimForApprovalViewModel MapToPendingClaimViewModel(Claim claim)
    {
        var daysPending = (DateTime.UtcNow - claim.SubmissionDate).Days;

        return new PendingClaimForApprovalViewModel
        {
            ClaimId = claim.ClaimId,
            ClaimNumber = claim.ClaimNumber,
            LecturerName = $"{claim.Lecturer.FirstName} {claim.Lecturer.LastName}",
            LecturerEmail = claim.Lecturer.Email,
            ClaimMonth = claim.ClaimMonth,
            SubmissionDate = claim.SubmissionDate,
            TotalHours = claim.TotalHours,
            TotalAmount = claim.TotalAmount,
            ItemCount = claim.ClaimItems.Count,
            DocumentCount = claim.Documents.Count,
            Programmes = claim.ClaimItems.Select(ci => ci.Module.Programme.ProgrammeName).Distinct().ToList(),
            Modules = claim.ClaimItems.Select(ci => ci.Module.ModuleName).ToList(),
            Items = claim.ClaimItems.Select(ci => new ClaimItemSummary
            {
                ModuleCode = ci.Module.ModuleCode,
                ModuleName = ci.Module.ModuleName,
                HoursWorked = ci.HoursWorked,
                HourlyRate = ci.HourlyRate,
                TotalAmount = ci.TotalAmount
            }).ToList(),
            Documents = claim.Documents.Select(d => new DocumentSummary
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                FileSize = FormatFileSize(d.FileSize),
                FileType = d.ContentType,
                Icon = GetFileIcon(d.ContentType)
            }).ToList(),
            DaysPending = daysPending,
            Priority = daysPending > 7 ? "High" : daysPending > 3 ? "Medium" : "Low",
            HasRequiredDocuments = claim.TotalAmount <= 10000 || claim.Documents.Any()
        };
    }

    private async Task<CoordinatorStatistics> CalculateCoordinatorStatistics(int coordinatorId, List<int> programmeIds)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return new CoordinatorStatistics
        {
            PendingReviewCount = await _context.Claims
                .CountAsync(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderCoordinatorReview) &&
                                 c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId))),
            ApprovedThisMonth = await _context.Claims
                .CountAsync(c => c.CoordinatorId == coordinatorId &&
                                 c.Status == ClaimStatus.CoordinatorApproved &&
                                 c.CoordinatorApprovalDate >= startOfMonth),
            RejectedThisMonth = await _context.Claims
                .CountAsync(c => c.CoordinatorId == coordinatorId &&
                                 c.Status == ClaimStatus.CoordinatorRejected &&
                                 c.CoordinatorApprovalDate >= startOfMonth),
            TotalAmountPending = await _context.Claims
                .Where(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderCoordinatorReview) &&
                               c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)))
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0,
            OverdueClaims = await _context.Claims
                .CountAsync(c => (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderCoordinatorReview) &&
                                 c.ClaimItems.Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)) &&
                                 EF.Functions.DateDiffDay(c.SubmissionDate, DateTime.UtcNow) > 7)
        };
    }

    private async Task<ManagerStatistics> CalculateManagerStatistics(int managerId)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return new ManagerStatistics
        {
            PendingReviewCount = await _context.Claims
                .CountAsync(c => c.Status == ClaimStatus.CoordinatorApproved || c.Status == ClaimStatus.UnderManagerReview),
            ApprovedThisMonth = await _context.Claims
                .CountAsync(c => c.ManagerId == managerId &&
                                 c.Status == ClaimStatus.ManagerApproved &&
                                 c.ManagerApprovalDate >= startOfMonth),
            RejectedThisMonth = await _context.Claims
                .CountAsync(c => c.ManagerId == managerId &&
                                 c.Status == ClaimStatus.ManagerRejected &&
                                 c.ManagerApprovalDate >= startOfMonth),
            TotalAmountPending = await _context.Claims
                .Where(c => c.Status == ClaimStatus.CoordinatorApproved || c.Status == ClaimStatus.UnderManagerReview)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0,
            TotalAmountApproved = await _context.Claims
                .Where(c => c.ManagerId == managerId &&
                               c.Status == ClaimStatus.ManagerApproved &&
                               c.ManagerApprovalDate >= startOfMonth)
                .SumAsync(c => (decimal?)c.TotalAmount) ?? 0,
            OverdueClaims = await _context.Claims
                .CountAsync(c => (c.Status == ClaimStatus.CoordinatorApproved || c.Status == ClaimStatus.UnderManagerReview) &&
                                 c.CoordinatorApprovalDate.HasValue &&
                                 EF.Functions.DateDiffDay(c.CoordinatorApprovalDate.Value, DateTime.UtcNow) > 5)
        };
    }

    // ==================================================================
    // == END: MERGED METHODS FROM SAMPLE 2
    // ==================================================================


    /// <summary>
    /// Extended service for claim validation and business rules
    /// </summary>
    public class ClaimValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClaimValidationService> _logger;

        public ClaimValidationService(ApplicationDbContext context, ILogger<ClaimValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Validates if a lecturer can submit a claim for the specified month
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateClaimSubmissionAsync(int lecturerId, DateTime claimMonth)
        {
            // Check if claim already exists for this month
            var existingClaim = await _context.Claims
                .AnyAsync(c => c.LecturerId == lecturerId &&
                               c.ClaimMonth.Year == claimMonth.Year &&
                               c.ClaimMonth.Month == claimMonth.Month &&
                               c.Status != ClaimStatus.Cancelled);

            if (existingClaim)
                return (false, "A claim already exists for this month");

            // Check if claim month is not in the future
            if (claimMonth > DateTime.UtcNow)
                return (false, "Cannot submit claims for future months");

            // Check if claim is not too old (e.g., more than 3 months ago)
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
            if (claimMonth < threeMonthsAgo)
                return (false, "Cannot submit claims older than 3 months");

            return (true, string.Empty);
        }

        /// <summary>
        /// Validates claim items before submission
        /// </summary>
        public async Task<(bool IsValid, List<string> Errors)> ValidateClaimItemsAsync(int lecturerId, List<ClaimItemViewModel> items)
        {
            var errors = new List<string>();

            if (items == null || !items.Any())
            {
                errors.Add("At least one claim item is required");
                return (false, errors);
            }

            foreach (var item in items)
            {
                // Validate hours worked
                if (item.HoursWorked <= 0)
                {
                    errors.Add($"Hours worked must be greater than 0 for module {item.ModuleCode}");
                }

                if (item.HoursWorked > 200)
                {
                    errors.Add($"Hours worked cannot exceed 200 per month for module {item.ModuleCode}");
                }

                // Validate module assignment
                var isAssigned = await _context.LecturerModules
                    .AnyAsync(lm => lm.LecturerId == lecturerId &&
                                   lm.ModuleId == item.ModuleId &&
                                   lm.IsActive);

                if (!isAssigned)
                {
                    errors.Add($"You are not assigned to teach module {item.ModuleCode}");
                }

                // Validate hourly rate
                var module = await _context.Modules.FindAsync(item.ModuleId);
                if (module != null && item.HourlyRate != module.HourlyRate)
                {
                    errors.Add($"Hourly rate for module {item.ModuleCode} does not match the system rate");
                }

                // Validate work date
                if (item.WorkDate > DateTime.Today)
                {
                    errors.Add($"Work date cannot be in the future for module {item.ModuleCode}");
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// Validates if a claim can be approved by the current user
        /// </summary>
        public async Task<(bool CanApprove, string Reason)> CanUserApproveClaimAsync(int userId, int claimId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return (false, "User not found");

            var claim = await _context.Claims
                .Include(c => c.ClaimItems)
                    .ThenInclude(ci => ci.Module)
                        .ThenInclude(m => m.Programme)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
                return (false, "Claim not found");

            // Programme Coordinator validation
            if (user.UserType == UserType.ProgrammeCoordinator)
            {
                if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderCoordinatorReview)
                    return (false, "Claim is not in a valid state for coordinator approval");

                var coordinatorProgrammes = await _context.Programmes
                    .Where(p => p.CoordinatorId == userId)
                    .Select(p => p.ProgrammeId)
                    .ToListAsync();

                var claimProgrammes = claim.ClaimItems
                    .Select(ci => ci.Module.ProgrammeId)
                    .Distinct()
                    .ToList();

                var hasAuthorization = claimProgrammes.All(pid => coordinatorProgrammes.Contains(pid));

                if (!hasAuthorization)
                    return (false, "You are not authorized to approve claims for all modules in this claim");

                return (true, string.Empty);
            }

            // Academic Manager validation
            if (user.UserType == UserType.AcademicManager)
            {
                if (claim.Status != ClaimStatus.CoordinatorApproved && claim.Status != ClaimStatus.UnderManagerReview)
                    return (false, "Claim must be approved by Programme Coordinator first");

                return (true, string.Empty);
            }

            return (false, "You do not have permission to approve claims");
        }

        /// <summary>
        /// Calculates total claim statistics for validation
        /// </summary>
        public async Task<ClaimStatistics> CalculateClaimStatisticsAsync(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.ClaimItems)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
                return new ClaimStatistics();

            return new ClaimStatistics
            {
                TotalItems = claim.ClaimItems.Count,
                TotalHours = claim.ClaimItems.Sum(ci => ci.HoursWorked),
                TotalAmount = claim.ClaimItems.Sum(ci => ci.TotalAmount),
                AverageHourlyRate = claim.ClaimItems.Average(ci => ci.HourlyRate),
                MinHourlyRate = claim.ClaimItems.Min(ci => ci.HourlyRate),
                MaxHourlyRate = claim.ClaimItems.Max(ci => ci.HourlyRate),
                UniqueModules = claim.ClaimItems.Select(ci => ci.ModuleId).Distinct().Count()
            };
        }
    }

    /// <summary>
    /// Service for claim workflow management
    /// </summary>
    public class ClaimWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ClaimWorkflowService> _logger;

        public ClaimWorkflowService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<ClaimWorkflowService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Advances claim to next workflow stage
        /// </summary>
        public async Task<bool> AdvanceClaimWorkflowAsync(int claimId, int userId)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            ClaimStatus? nextStatus = null;

            switch (claim.Status)
            {
                case ClaimStatus.Draft:
                    nextStatus = ClaimStatus.Submitted;
                    break;
                case ClaimStatus.Submitted:
                    if (user.UserType == UserType.ProgrammeCoordinator)
                        nextStatus = ClaimStatus.UnderCoordinatorReview;
                    break;
                case ClaimStatus.CoordinatorApproved:
                    if (user.UserType == UserType.AcademicManager)
                        nextStatus = ClaimStatus.UnderManagerReview;
                    break;
                case ClaimStatus.ManagerApproved:
                    nextStatus = ClaimStatus.Paid;
                    break;
            }

            if (nextStatus.HasValue)
            {
                var previousStatus = claim.Status;
                claim.Status = nextStatus.Value;
                claim.LastModifiedDate = DateTime.UtcNow;

                var statusHistory = new ClaimStatusHistory
                {
                    ClaimId = claimId,
                    PreviousStatus = previousStatus,
                    NewStatus = nextStatus.Value,
                    StatusChangeDate = DateTime.UtcNow,
                    ChangedByUserId = userId,
                    Comments = "Workflow advanced automatically",
                    SystemNotes = $"Status changed from {previousStatus} to {nextStatus.Value}"
                };

                _context.ClaimStatusHistories.Add(statusHistory);
                await _context.SaveChangesAsync();

                await _notificationService.SendClaimStatusNotificationAsync(claimId, nextStatus.Value);

                _logger.LogInformation("Claim {ClaimId} advanced from {PreviousStatus} to {NewStatus}",
                    claimId, previousStatus, nextStatus.Value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Cancels a claim
        /// </summary>
        public async Task<bool> CancelClaimAsync(int claimId, int userId, string reason)
        {
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null) return false;

            // Only draft and rejected claims can be cancelled
            if (claim.Status != ClaimStatus.Draft &&
                claim.Status != ClaimStatus.CoordinatorRejected &&
                claim.Status != ClaimStatus.ManagerRejected)
            {
                return false;
            }

            var previousStatus = claim.Status;
            claim.Status = ClaimStatus.Cancelled;
            claim.LastModifiedDate = DateTime.UtcNow;

            var statusHistory = new ClaimStatusHistory
            {
                ClaimId = claimId,
                PreviousStatus = previousStatus,
                NewStatus = ClaimStatus.Cancelled,
                StatusChangeDate = DateTime.UtcNow,
                ChangedByUserId = userId,
                Comments = reason,
                SystemNotes = "Claim cancelled by user"
            };

            _context.ClaimStatusHistories.Add(statusHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Claim {ClaimId} cancelled by user {UserId}", claimId, userId);

            return true;
        }

        /// <summary>
        /// Gets the next approver for a claim
        /// </summary>
        public async Task<User?> GetNextApproverAsync(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.ClaimItems)
                    .ThenInclude(ci => ci.Module)
                        .ThenInclude(m => m.Programme)
                            .ThenInclude(p => p.Coordinator)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return null;

            switch (claim.Status)
            {
                case ClaimStatus.Submitted:
                case ClaimStatus.UnderCoordinatorReview:
                    // Get the coordinator for the first programme
                    var firstProgramme = claim.ClaimItems.FirstOrDefault()?.Module.Programme;
                    return firstProgramme?.Coordinator;

                case ClaimStatus.CoordinatorApproved:
                case ClaimStatus.UnderManagerReview:
                    // Get any academic manager
                    return await _context.AcademicManagers.FirstOrDefaultAsync();

                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if claim has all required documents
        /// </summary>
        public async Task<bool> HasRequiredDocumentsAsync(int claimId)
        {
            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return false;

            // Check if claim amount requires supporting documents
            // For example, claims over R10,000 require documents
            if (claim.TotalAmount > 10000)
            {
                return claim.Documents.Any();
            }

            return true; // No documents required for smaller claims
        }
    }
}