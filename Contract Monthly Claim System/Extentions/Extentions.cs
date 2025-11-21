using CMCS.Services.Implementation;
using Contract_Monthly_Claim_System.Services.Implementation;
using Contract_Monthly_Claim_System.Services.Interfaces;
using static ClaimService;

namespace Contract_Monthly_Claim_System.Extentions
{
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all claim-related services
        /// </summary>
        public static IServiceCollection AddClaimServices(this IServiceCollection services)
        {
            services.AddScoped<IClaimService, ClaimService>();
            services.AddScoped<ClaimValidationService>();
            services.AddScoped<ClaimWorkflowService>();

            return services;
        }

        /// <summary>
        /// Registers all application services
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IClaimService, ClaimService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IProgrammeService, ProgrammeService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<INotificationService, EnhancedNotificationService>();
            services.AddScoped<IEmailSender, EmailSender>();

            // Additional services
            services.AddScoped<ClaimValidationService>();
            services.AddScoped<ClaimWorkflowService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IFileValidationService, FileValidationService>();
            services.AddScoped<IPdfGenerationService, PdfGenerationService>();

            return services;
        }
    }

    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers document and user services
        /// </summary>
        public static IServiceCollection AddDocumentAndUserServices(this IServiceCollection services)
        {
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IFileValidationService, FileValidationService>();

            return services;
        }
    }


}
