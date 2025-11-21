using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services.Implementation
{
    public class ModuleService : IModuleService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ModuleService> _logger;

        public ModuleService(ApplicationDbContext context, ILogger<ModuleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ModuleSelectViewModel>> GetLecturerModulesAsync(int lecturerId)
        {
            return await _context.LecturerModules
                .Where(lm => lm.LecturerId == lecturerId && lm.IsActive)
                .Include(lm => lm.Module)
                    .ThenInclude(m => m.Programme)
                .Select(lm => new ModuleSelectViewModel
                {
                    ModuleId = lm.Module.ModuleId,
                    ModuleName = lm.Module.ModuleName,
                    ModuleCode = lm.Module.ModuleCode,
                    ProgrammeName = lm.Module.Programme.ProgrammeName,
                    HourlyRate = lm.Module.HourlyRate,
                    IsAssigned = true
                })
                .OrderBy(m => m.ModuleCode)
                .ToListAsync();
        }

        public async Task<List<ModuleSelectViewModel>> GetModulesAsync(int? programmeId = null)
        {
            var query = _context.Modules
                .Include(m => m.Programme)
                .Where(m => m.IsActive);

            if (programmeId.HasValue)
            {
                query = query.Where(m => m.ProgrammeId == programmeId.Value);
            }

            return await query
                .Select(m => new ModuleSelectViewModel
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName,
                    ModuleCode = m.ModuleCode,
                    ProgrammeName = m.Programme.ProgrammeName,
                    HourlyRate = m.HourlyRate,
                    IsAssigned = false
                })
                .OrderBy(m => m.ProgrammeName)
                .ThenBy(m => m.ModuleCode)
                .ToListAsync();
        }

        public async Task<Module?> GetModuleByIdAsync(int moduleId)
        {
            return await _context.Modules
                .Include(m => m.Programme)
                .FirstOrDefaultAsync(m => m.ModuleId == moduleId);
        }

        public async Task<List<Module>> GetModulesByProgrammeAsync(int programmeId)
        {
            return await _context.Modules
                .Where(m => m.ProgrammeId == programmeId && m.IsActive)
                .OrderBy(m => m.ModuleCode)
                .ToListAsync();
        }

        public async Task AssignLecturerToModuleAsync(int lecturerId, int moduleId)
        {
            var existingAssignment = await _context.LecturerModules
                .FirstOrDefaultAsync(lm => lm.LecturerId == lecturerId && lm.ModuleId == moduleId);

            if (existingAssignment != null)
            {
                existingAssignment.IsActive = true;
                existingAssignment.AssignedDate = DateTime.UtcNow;
            }
            else
            {
                var assignment = new LecturerModule
                {
                    LecturerId = lecturerId,
                    ModuleId = moduleId,
                    AssignedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.LecturerModules.Add(assignment);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Lecturer {LecturerId} assigned to module {ModuleId}", lecturerId, moduleId);
        }

        public async Task RemoveLecturerFromModuleAsync(int lecturerId, int moduleId)
        {
            var assignment = await _context.LecturerModules
                .FirstOrDefaultAsync(lm => lm.LecturerId == lecturerId && lm.ModuleId == moduleId);

            if (assignment != null)
            {
                assignment.IsActive = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Lecturer {LecturerId} removed from module {ModuleId}", lecturerId, moduleId);
            }
        }

        
    }

    public class ProgrammeService : IProgrammeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProgrammeService> _logger;

        public ProgrammeService(ApplicationDbContext context, ILogger<ProgrammeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Programme>> GetAllProgrammesAsync()
        {
            return await _context.Programmes
                .Include(p => p.Coordinator)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
        }

        public async Task<List<Programme>> GetCoordinatorProgrammesAsync(int coordinatorId)
        {
            return await _context.Programmes
                .Where(p => p.CoordinatorId == coordinatorId && p.IsActive)
                .OrderBy(p => p.ProgrammeName)
                .ToListAsync();
        }

        public async Task<Programme?> GetProgrammeByIdAsync(int programmeId)
        {
            return await _context.Programmes
                .Include(p => p.Coordinator)
                .Include(p => p.Modules.Where(m => m.IsActive))
                .FirstOrDefaultAsync(p => p.ProgrammeId == programmeId);
        }

        public async Task<Programme> CreateProgrammeAsync(Programme programme)
        {
            programme.CreatedDate = DateTime.UtcNow;
            _context.Programmes.Add(programme);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Programme {ProgrammeName} created with ID {ProgrammeId}",
                programme.ProgrammeName, programme.ProgrammeId);

            return programme;
        }

        public async Task UpdateProgrammeAsync(Programme programme)
        {
            _context.Programmes.Update(programme);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Programme {ProgrammeId} updated", programme.ProgrammeId);
        }

        public async Task DeleteProgrammeAsync(int programmeId)
        {
            var programme = await _context.Programmes.FindAsync(programmeId);
            if (programme != null)
            {
                programme.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Programme {ProgrammeId} deactivated", programmeId);
            }
        }
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MonthlyReportViewModel> GenerateMonthlyReportAsync(DateTime month, int userId)
        {
            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.ClaimItems)
                .Where(c => c.ClaimMonth >= startDate && c.ClaimMonth <= endDate)
                .Where(c => c.Status == ClaimStatus.ManagerApproved || c.Status == ClaimStatus.Paid)
                .ToListAsync();

            var lecturerSummaries = claims
                .GroupBy(c => c.Lecturer)
                .Select(g => new LecturerSummaryViewModel
                {
                    LecturerName = $"{g.Key.FirstName} {g.Key.LastName}",
                    EmployeeNumber = ((Lecturer)g.Key).EmployeeNumber,
                    TotalClaims = g.Count(),
                    TotalHours = g.Sum(c => c.TotalHours),
                    TotalAmount = g.Sum(c => c.TotalAmount),
                    AverageHourlyRate = g.Sum(c => c.TotalAmount) / g.Sum(c => c.TotalHours)
                })
                .OrderBy(ls => ls.LecturerName)
                .ToList();

            var currentUser = await _context.Users.FindAsync(userId);

            return new MonthlyReportViewModel
            {
                ReportTitle = $"Monthly Claims Report - {month:MMMM yyyy}",
                GeneratedDate = DateTime.Now,
                GeneratedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "System",
                ReportMonth = month,
                LecturerSummaries = lecturerSummaries,
                TotalPaid = claims.Sum(c => c.TotalAmount),
                TotalHours = claims.Sum(c => c.TotalHours),
                TotalClaims = claims.Count
            };
        }

        public async Task<LecturerSummaryViewModel> GenerateLecturerReportAsync(int? lecturerId, DateTime fromDate, DateTime toDate, int userId)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.ClaimItems)
                .Where(c => c.SubmissionDate >= fromDate && c.SubmissionDate <= toDate)
                .Where(c => c.Status == ClaimStatus.ManagerApproved || c.Status == ClaimStatus.Paid);

            if (lecturerId.HasValue)
            {
                query = query.Where(c => c.LecturerId == lecturerId.Value);
            }

            var claims = await query.ToListAsync();

            if (!claims.Any())
            {
                return new LecturerSummaryViewModel
                {
                    LecturerName = "No Data",
                    EmployeeNumber = "N/A",
                    TotalClaims = 0,
                    TotalHours = 0,
                    TotalAmount = 0,
                    AverageHourlyRate = 0
                };
            }

            var firstClaim = claims.First();
            var totalHours = claims.Sum(c => c.TotalHours);
            var totalAmount = claims.Sum(c => c.TotalAmount);

            return new LecturerSummaryViewModel
            {
                LecturerName = $"{firstClaim.Lecturer.FirstName} {firstClaim.Lecturer.LastName}",
                EmployeeNumber = ((Lecturer)firstClaim.Lecturer).EmployeeNumber,
                TotalClaims = claims.Count,
                TotalHours = totalHours,
                TotalAmount = totalAmount,
                AverageHourlyRate = totalHours > 0 ? totalAmount / totalHours : 0
            };
        }

        public async Task<ReportViewModel> GenerateProgrammeReportAsync(int? programmeId, DateTime fromDate, DateTime toDate, int userId)
        {
            var query = _context.ClaimItems
                .Include(ci => ci.Module)
                    .ThenInclude(m => m.Programme)
                .Include(ci => ci.Claim)
                    .ThenInclude(c => c.Lecturer)
                .Where(ci => ci.Claim.SubmissionDate >= fromDate && ci.Claim.SubmissionDate <= toDate)
                .Where(ci => ci.Claim.Status == ClaimStatus.ManagerApproved || ci.Claim.Status == ClaimStatus.Paid);

            if (programmeId.HasValue)
            {
                query = query.Where(ci => ci.Module.ProgrammeId == programmeId.Value);
            }

            var claimItems = await query.ToListAsync();
            var currentUser = await _context.Users.FindAsync(userId);

            var reportData = new Dictionary<string, object>
            {
                ["ClaimItems"] = claimItems,
                ["TotalAmount"] = claimItems.Sum(ci => ci.TotalAmount),
                ["TotalHours"] = claimItems.Sum(ci => ci.HoursWorked),
                ["UniqueLecturers"] = claimItems.Select(ci => ci.Claim.LecturerId).Distinct().Count(),
                ["FromDate"] = fromDate,
                ["ToDate"] = toDate
            };

            return new ReportViewModel
            {
                ReportTitle = $"Programme Claims Report - {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                GeneratedDate = DateTime.Now,
                GeneratedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "System",
                Parameters = reportData
            };
        }

        public async Task<byte[]> ExportReportAsync(string reportType, string format, Dictionary<string, object> parameters, int userId)
        {
            // This is a placeholder implementation
            // In a real application, you would use libraries like:
            // - iTextSharp or PdfSharp for PDF generation
            // - EPPlus or ClosedXML for Excel generation
            // - CsvHelper for CSV generation

            switch (format.ToLower())
            {
                case "pdf":
                    return await GeneratePdfReportAsync(reportType, parameters);
                case "excel":
                    return await GenerateExcelReportAsync(reportType, parameters);
                case "csv":
                    return await GenerateCsvReportAsync(reportType, parameters);
                default:
                    throw new ArgumentException($"Unsupported format: {format}");
            }
        }

        private async Task<byte[]> GeneratePdfReportAsync(string reportType, Dictionary<string, object> parameters)
        {
            // PDF generation logic would go here
            // Using libraries like iTextSharp or similar
            await Task.Delay(1); // Placeholder
            return Array.Empty<byte>();
        }

        private async Task<byte[]> GenerateExcelReportAsync(string reportType, Dictionary<string, object> parameters)
        {
            // Excel generation logic would go here
            // Using libraries like EPPlus or ClosedXML
            await Task.Delay(1); // Placeholder
            return Array.Empty<byte>();
        }

        private async Task<byte[]> GenerateCsvReportAsync(string reportType, Dictionary<string, object> parameters)
        {
            // CSV generation logic would go here
            // Using CsvHelper or similar
            await Task.Delay(1); // Placeholder
            return Array.Empty<byte>();
        }
    }

    public class EnhancedNotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EnhancedNotificationService> _logger;
        private readonly IEmailSender _emailSender; // You would implement this

        public EnhancedNotificationService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<EnhancedNotificationService> logger,
            IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailSender = emailSender;
        }

        public async Task<List<NotificationViewModel>> GetRecentNotificationsAsync(int userId, int count)
        {
            // This would query a Notifications table if you had one
            // For now, we'll generate some sample notifications based on recent claim activities

            var recentClaims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.LecturerId == userId ||
                           (c.CoordinatorId == userId) ||
                           (c.ManagerId == userId))
                .OrderByDescending(c => c.LastModifiedDate)
                .Take(count)
                .ToListAsync();

            return recentClaims.Select(c => new NotificationViewModel
            {
                Title = GetNotificationTitle(c.Status),
                Message = $"Claim {c.ClaimNumber} - {c.TotalAmount:C}",
                Type = GetNotificationType(c.Status),
                CreatedDate = c.LastModifiedDate,
                Icon = GetNotificationIcon(c.Status),
                IsRead = false
            }).ToList();
        }

        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            // This would query actual notifications table
            // For now, return a sample count
            var pendingClaims = await _context.Claims
                .CountAsync(c => c.LecturerId == userId &&
                                (c.Status == ClaimStatus.UnderCoordinatorReview ||
                                 c.Status == ClaimStatus.UnderManagerReview));

            return pendingClaims;
        }

        public async Task SendClaimStatusNotificationAsync(int claimId, ClaimStatus newStatus, string comments = "")
        {
            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null) return;

            var emailSettings = _configuration.GetSection("EmailSettings");
            if (!emailSettings.Exists()) return;

            var subject = $"CMCS - Claim {claim.ClaimNumber} Status Update";
            var body = GenerateEmailBody(claim, newStatus, comments);

            try
            {
                await _emailSender.SendEmailAsync(claim.Lecturer.Email, subject, body);
                _logger.LogInformation("Status notification sent for claim {ClaimId} to {Email}", claimId, claim.Lecturer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status notification for claim {ClaimId}", claimId);
            }
        }

        public async Task SendEmailNotificationAsync(string toEmail, string subject, string body)
        {
            try
            {
                await _emailSender.SendEmailAsync(toEmail, subject, body);
                _logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            // Implementation would update notifications table
            await Task.Delay(1); // Placeholder
            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }

        private string GetNotificationTitle(ClaimStatus status)
        {
            return status switch
            {
                ClaimStatus.Submitted => "Claim Submitted",
                ClaimStatus.CoordinatorApproved => "Claim Approved by Coordinator",
                ClaimStatus.ManagerApproved => "Claim Approved by Manager",
                ClaimStatus.CoordinatorRejected => "Claim Rejected by Coordinator",
                ClaimStatus.ManagerRejected => "Claim Rejected by Manager",
                ClaimStatus.Paid => "Claim Payment Processed",
                _ => "Claim Status Update"
            };
        }

        private string GetNotificationType(ClaimStatus status)
        {
            return status switch
            {
                ClaimStatus.CoordinatorApproved or ClaimStatus.ManagerApproved or ClaimStatus.Paid => "success",
                ClaimStatus.CoordinatorRejected or ClaimStatus.ManagerRejected => "danger",
                ClaimStatus.Submitted => "info",
                _ => "secondary"
            };
        }

        private string GetNotificationIcon(ClaimStatus status)
        {
            return status switch
            {
                ClaimStatus.CoordinatorApproved or ClaimStatus.ManagerApproved => "fas fa-check-circle",
                ClaimStatus.CoordinatorRejected or ClaimStatus.ManagerRejected => "fas fa-times-circle",
                ClaimStatus.Submitted => "fas fa-paper-plane",
                ClaimStatus.Paid => "fas fa-dollar-sign",
                _ => "fas fa-info-circle"
            };
        }

        private string GenerateEmailBody(Claim claim, ClaimStatus newStatus, string comments)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #2c3e50, #3498db); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h2 style='margin: 0;'>Contract Monthly Claim System</h2>
                            <p style='margin: 10px 0 0 0;'>Claim Status Update</p>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6;'>
                            <h3 style='color: #2c3e50; margin-top: 0;'>Claim Details</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px 0; font-weight: bold;'>Claim Number:</td>
                                    <td style='padding: 8px 0;'>{claim.ClaimNumber}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0; font-weight: bold;'>Claim Month:</td>
                                    <td style='padding: 8px 0;'>{claim.ClaimMonth:MMMM yyyy}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0; font-weight: bold;'>Amount:</td>
                                    <td style='padding: 8px 0;'>{claim.TotalAmount:C}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0; font-weight: bold;'>New Status:</td>
                                    <td style='padding: 8px 0; color: #e74c3c; font-weight: bold;'>{newStatus.ToString().Replace("_", " ")}</td>
                                </tr>
                            </table>
                        </div>
                        
                        {(!string.IsNullOrEmpty(comments) ? $@"
                        <div style='background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; margin: 10px 0;'>
                            <h4 style='margin-top: 0; color: #856404;'>Comments:</h4>
                            <p style='margin-bottom: 0;'>{comments}</p>
                        </div>
                        " : "")}
                        
                        <div style='background: white; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 10px 10px;'>
                            <p>Please log into the CMCS system to view complete details and take any necessary actions.</p>
                            <div style='text-align: center; margin: 20px 0;'>
                                <a href='#' style='background: #3498db; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    View Claim Details
                                </a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'>
                            <p style='font-size: 12px; color: #666; margin: 0;'>
                                This is an automated message from the Contract Monthly Claim System. Please do not reply to this email.
                            </p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        public Task NotifyClaimSubmittedAsync(int claimId)
        {
            throw new NotImplementedException();
        }
    }


    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailAsync(email, subject, htmlMessage, null);
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string plainTextMessage)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            if (!emailSettings.Exists())
            {
                _logger.LogWarning("Email settings not configured. Skipping email send to {Email}", email);
                return;
            }

            try
            {
                // This is where you would implement actual email sending
                // Using services like SendGrid, SMTP, Amazon SES, etc.

                // Example with SMTP:
                /*
                using var smtpClient = new SmtpClient(emailSettings["SmtpServer"], emailSettings.GetValue<int>("SmtpPort"));
                smtpClient.Credentials = new NetworkCredential(emailSettings["Username"], emailSettings["Password"]);
                smtpClient.EnableSsl = emailSettings.GetValue<bool>("UseSSL");

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(emailSettings["SenderEmail"], emailSettings["SenderName"]),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                
                mailMessage.To.Add(email);
                
                if (!string.IsNullOrEmpty(plainTextMessage))
                {
                    mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextMessage, null, "text/plain"));
                }

                await smtpClient.SendMailAsync(mailMessage);
                */

                // For now, just log that we would send the email
                _logger.LogInformation("Email would be sent to {Email} with subject: {Subject}", email, subject);

                // Simulate async operation
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                throw;
            }
        }
    }



}



