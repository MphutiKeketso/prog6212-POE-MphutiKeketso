using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IProgrammeService
    {
        Task<List<Programme>> GetAllProgrammesAsync();
        Task<List<Programme>> GetCoordinatorProgrammesAsync(int coordinatorId);
        Task<Programme?> GetProgrammeByIdAsync(int programmeId);
        Task<Programme> CreateProgrammeAsync(Programme programme);
        Task UpdateProgrammeAsync(Programme programme);
        Task DeleteProgrammeAsync(int programmeId);
    }
}
