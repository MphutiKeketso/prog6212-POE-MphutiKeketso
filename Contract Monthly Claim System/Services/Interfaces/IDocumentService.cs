using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IDocumentService
    {
        // Document retrieval
        Task<List<DocumentViewModel>> GetUserDocumentsAsync(int userId);
        Task<List<DocumentViewModel>> GetClaimDocumentsAsync(int claimId);
        Task<Document?> GetDocumentAsync(int DocumentId);
        Task<byte[]> GetDocumentContentAsync(int DocumentId);
        Task<DocumentMetadata> GetDocumentMetadataAsync(int DocumentId);

        // Document upload
        Task<List<Document>> UploadDocumentsAsync(int claimId, List<IFormFile> files, string description = "");
        Task<Document> UploadSingleDocumentAsync(int claimId, IFormFile file, string description = "");
        Task<bool> ReplaceDocumentAsync(int DocumentId, IFormFile newFile);

        // Document deletion
        Task DeleteDocumentAsync(int DocumentId, int userId);
        Task<int> DeleteClaimDocumentsAsync(int claimId);

        // Validation
        Task<bool> ValidateFileAsync(IFormFile file);
        Task<(bool IsValid, List<string> Errors)> ValidateDocumentsForClaimAsync(int claimId);
    }
}
