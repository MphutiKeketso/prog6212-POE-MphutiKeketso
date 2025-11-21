using Contract_Monthly_Claim_System.Models.View;
using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IModuleService
    {
        Task<List<ModuleSelectViewModel>> GetLecturerModulesAsync(int lecturerId);
        Task<List<ModuleSelectViewModel>> GetModulesAsync(int? programmeId = null);
        Task<Module?> GetModuleByIdAsync(int moduleId);
        Task<List<Module>> GetModulesByProgrammeAsync(int programmeId);
        Task AssignLecturerToModuleAsync(int lecturerId, int moduleId);
        Task RemoveLecturerFromModuleAsync(int lecturerId, int moduleId);
    }
}
