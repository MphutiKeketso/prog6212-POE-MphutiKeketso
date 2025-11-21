using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using DocumentMetadata = Contract_Monthly_Claim_System.Models.View.DocumentMetadata;

namespace Contract_Monthly_Claim_System.Services.Implementation
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentService> _logger;
        private readonly IFileValidationService _fileValidationService;

        public DocumentService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<DocumentService> logger,
            IFileValidationService fileValidationService)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _fileValidationService = fileValidationService;
        }

        // ===============================================
        // DOCUMENT RETRIEVAL METHODS
        // ===============================================

        public async Task<List<DocumentViewModel>> GetUserDocumentsAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return new List<DocumentViewModel>();

            IQueryable<Document> query = _context.Documents
                .Include(d => d.Claim)
                    .ThenInclude(c => c.Lecturer);

            // Filter based on user type
            if (user.UserType == UserType.Lecturer)
            {
                query = query.Where(d => d.Claim.LecturerId == userId);
            }
            else if (user.UserType == UserType.ProgrammeCoordinator)
            {
                var programmeIds = await _context.Programmes
                    .Where(p => p.CoordinatorId == userId)
                    .Select(p => p.ProgrammeId)
                    .ToListAsync();

                query = query.Where(d => d.Claim.ClaimItems
                    .Any(ci => programmeIds.Contains(ci.Module.ProgrammeId)));
            }
            // Academic Managers and Admins see all documents

            return await query
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new DocumentViewModel
                {
                    DocumentId = d.DocumentId,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    UploadDate = d.UploadDate,
                    Description = d.Description,
                    IsRequired = d.IsRequired,
                    UploadedByName = $"{d.UploadedBy.FirstName} {d.UploadedBy.LastName}",
                    FileSizeDisplay = FormatFileSize(d.FileSize),
                    FileIcon = GetFileIcon(d.ContentType)
                })
                .ToListAsync();
        }

        public async Task<List<DocumentViewModel>> GetClaimDocumentsAsync(int claimId)
        {
            var documents = await _context.Documents
                .Include(d => d.UploadedBy)
                .Where(d => d.ClaimId == claimId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return documents.Select(d => new DocumentViewModel
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadDate = d.UploadDate,
                Description = d.Description,
                IsRequired = d.IsRequired,
                UploadedByName = d.UploadedBy != null ? $"{d.UploadedBy.FirstName} {d.UploadedBy.LastName}" : "Unknown",
                FileSizeDisplay = FormatFileSize(d.FileSize),
                FileIcon = GetFileIcon(d.ContentType)
            }).ToList();
        }

        public async Task<Document?> GetDocumentAsync(int documentId)
        {
            return await _context.Documents
                .Include(d => d.Claim)
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
        }

        public async Task<byte[]> GetDocumentContentAsync(int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                throw new FileNotFoundException($"Document with ID {documentId} not found");

            if (!File.Exists(document.FilePath))
            {
                _logger.LogError("Physical file not found for document {DocumentId} at path {FilePath}",
                    documentId, document.FilePath);
                throw new FileNotFoundException($"Physical file not found for document {document.FileName}");
            }

            try
            {
                return await File.ReadAllBytesAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading document {DocumentId}", documentId);
                throw;
            }
        }

        // ===============================================
        // DOCUMENT UPLOAD METHODS
        // ===============================================

        public async Task<List<Document>> UploadDocumentsAsync(int claimId, List<IFormFile> files, string description = "")
        {
            // Validate claim exists
            var claim = await _context.Claims.FindAsync(claimId);
            if (claim == null)
                throw new ArgumentException("Claim not found");

            var uploadedFiles = new List<Document>();
            var uploadPath = GetUploadPath();

            // Ensure upload directory exists
            Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                // Validate file
                if (!await _fileValidationService.ValidateFileAsync(file))
                {
                    _logger.LogWarning("File {FileName} failed validation", file.FileName);
                    continue;
                }

                try
                {
                    // Generate unique filename to prevent conflicts
                    var fileExtension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Create document record
                    var document = new Document
                    {
                        ClaimId = claimId,
                        FileName = file.FileName,
                        FilePath = filePath,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        Description = description,
                        UploadDate = DateTime.UtcNow,
                        UploadedByUserId = claim.LecturerId,
                        IsRequired = DetermineIfRequired(claim.TotalAmount)
                    };

                    _context.Documents.Add(document);
                    uploadedFiles.Add(document);

                    _logger.LogInformation("Document {FileName} uploaded for claim {ClaimId}",
                        file.FileName, claimId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file {FileName} for claim {ClaimId}",
                        file.FileName, claimId);
                    throw new InvalidOperationException($"Failed to upload file {file.FileName}", ex);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("{Count} documents uploaded successfully for claim {ClaimId}",
                uploadedFiles.Count, claimId);

            return uploadedFiles;
        }

        public async Task<Document> UploadSingleDocumentAsync(int claimId, IFormFile file, string description = "")
        {
            var documents = await UploadDocumentsAsync(claimId, new List<IFormFile> { file }, description);
            return documents.FirstOrDefault() ?? throw new InvalidOperationException("Failed to upload document");
        }

        public async Task<bool> ReplaceDocumentAsync(int documentId, IFormFile newFile)
        {
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return false;

            try
            {
                // Validate new file
                if (!await _fileValidationService.ValidateFileAsync(newFile))
                {
                    _logger.LogWarning("New file {FileName} failed validation", newFile.FileName);
                    return false;
                }

                // Delete old physical file
                if (File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                }

                // Save new file
                var uploadPath = GetUploadPath();
                var fileExtension = Path.GetExtension(newFile.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newFile.CopyToAsync(stream);
                }

                // Update document record
                document.FileName = newFile.FileName;
                document.FilePath = filePath;
                document.ContentType = newFile.ContentType;
                document.FileSize = newFile.Length;
                document.UploadDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} replaced successfully", documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing document {DocumentId}", documentId);
                return false;
            }
        }

        // ===============================================
        // DOCUMENT DELETION METHODS
        // ===============================================

        public async Task DeleteDocumentAsync(int documentId, int userId)
        {
            var document = await _context.Documents
                .Include(d => d.Claim)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
            {
                _logger.LogWarning("Attempt to delete non-existent document {DocumentId}", documentId);
                return;
            }

            // Verify user has permission to delete
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            // Only lecturer who owns the claim or admin can delete
            if (user.UserType == UserType.Lecturer && document.Claim.LecturerId != userId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this document");
            }

            // Don't allow deletion if claim is approved
            if (document.Claim.Status == ClaimStatus.ManagerApproved ||
                document.Claim.Status == ClaimStatus.Paid)
            {
                throw new InvalidOperationException("Cannot delete documents from approved claims");
            }

            try
            {
                // Delete physical file
                if (File.Exists(document.FilePath))
                {
                    File.Delete(document.FilePath);
                    _logger.LogInformation("Physical file deleted for document {DocumentId}", documentId);
                }

                // Delete database record
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {DocumentId} deleted by user {UserId}", documentId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<int> DeleteClaimDocumentsAsync(int claimId)
        {
            var documents = await _context.Documents
                .Where(d => d.ClaimId == claimId)
                .ToListAsync();

            int deletedCount = 0;

            foreach (var document in documents)
            {
                try
                {
                    // Delete physical file
                    if (File.Exists(document.FilePath))
                    {
                        File.Delete(document.FilePath);
                    }

                    _context.Documents.Remove(document);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting document {DocumentId}", document.DocumentId);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Count} documents deleted for claim {ClaimId}", deletedCount, claimId);

            return deletedCount;
        }

        // ===============================================
        // VALIDATION METHODS
        // ===============================================

        public async Task<bool> ValidateFileAsync(IFormFile file)
        {
            return await _fileValidationService.ValidateFileAsync(file);
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateDocumentsForClaimAsync(int claimId)
        {
            var errors = new List<string>();

            var claim = await _context.Claims
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);

            if (claim == null)
            {
                errors.Add("Claim not found");
                return (false, errors);
            }

            // Check if documents are required based on claim amount
            if (claim.TotalAmount > 10000 && !claim.Documents.Any())
            {
                errors.Add("Claims over R10,000 require supporting documents");
            }

            // Validate each document
            foreach (var document in claim.Documents)
            {
                if (!File.Exists(document.FilePath))
                {
                    errors.Add($"Physical file missing for document: {document.FileName}");
                }

                if (document.FileSize == 0)
                {
                    errors.Add($"Document {document.FileName} has zero size");
                }
            }

            return (errors.Count == 0, errors);
        }

        // ===============================================
        // HELPER METHODS
        // ===============================================

        private string GetUploadPath()
        {
            var uploadPath = _configuration["FileUploadSettings:UploadPath"];
            if (string.IsNullOrEmpty(uploadPath))
            {
                uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "documents");
            }
            else if (!Path.IsPathRooted(uploadPath))
            {
                uploadPath = Path.Combine(_environment.ContentRootPath, uploadPath);
            }

            return uploadPath;
        }

        private bool DetermineIfRequired(decimal claimAmount)
        {
            // Documents required for claims over R10,000
            return claimAmount > 10000;
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
                "image/jpeg" or "image/jpg" or "image/png" => "fas fa-file-image text-info",
                _ => "fas fa-file text-secondary"
            };
        }

        public async Task<DocumentMetadata> GetDocumentMetadataAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Claim)
                .Include(d => d.UploadedBy)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null)
                return null;

            return new DocumentMetadata
            {
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                FileSize = document.FileSize,
                ContentType = document.ContentType,
                UploadDate = document.UploadDate,
                ClaimNumber = document.Claim.ClaimNumber,
                UploadedBy = $"{document.UploadedBy.FirstName} {document.UploadedBy.LastName}",
                Description = document.Description,
                IsRequired = document.IsRequired,
                FileExists = File.Exists(document.FilePath)
            };
        }

        
    }
}
