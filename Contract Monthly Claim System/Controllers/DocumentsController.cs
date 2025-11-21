using Contract_Monthly_Claim_System.Services.Interfaces;
using Contract_Monthly_Claim_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contract_Monthly_Claim_System.Models.View;

namespace Contract_Monthly_Claim_System.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            IClaimService claimService,
            IUserService userService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _claimService = claimService;
            _userService = userService;
            _logger = logger;
        }

        // GET: Documents
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userService.GetCurrentUserAsync(User);
            var documents = await _documentService.GetUserDocumentsAsync(currentUser.UserId);

            return View(documents);
        }

        // POST: Documents/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int claimId, List<IFormFile> files, string description = "")
        {
            if (files == null || !files.Any())
            {
                return Json(new { success = false, message = "No files selected." });
            }

            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                var uploadedFiles = await _documentService.UploadDocumentsAsync(claimId, files, description);

                return Json(new
                {
                    success = true,
                    message = $"{uploadedFiles.Count} file(s) uploaded successfully!"
                    
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading documents for claim {ClaimId}", claimId);
                return Json(new { success = false, message = "Error uploading files. Please try again." });
            }
        }

        // GET: Documents/Download/5
        
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var document = await _documentService.GetDocumentAsync(id);
                if (document == null)
                {
                    return NotFound();
                }



                var fileBytes = await _documentService.GetDocumentContentAsync(id);
                return File(fileBytes, document.ContentType, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document {DocumentId}", id);
                return BadRequest("Error downloading file.");
            }
        }

        // DELETE: Documents/Delete/5
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync(User);
                await _documentService.DeleteDocumentAsync(id, currentUser.UserId);

                return Json(new { success = true, message = "Document deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return Json(new { success = false, message = "Error deleting document. Please try again." });
            }
        }
    }
}
