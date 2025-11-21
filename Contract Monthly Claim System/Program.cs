using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models.Configuration;
using Contract_Monthly_Claim_System.Services.Implementation;
using Contract_Monthly_Claim_System.Services.Interfaces;
using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
//using Contract_Monthly_Claim_System.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using CMCS.Services.Implementation;
using EmailSettings = Contract_Monthly_Claim_System.Models.EmailSettings;
using FileUploadSettings = Contract_Monthly_Claim_System.Models.FileUploadSettings;
using JwtSettings = Contract_Monthly_Claim_System.Models.JwtSettings;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===============================================
// 1. DATABASE CONFIGURATION
// ===============================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    
    
    
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ===============================================
// 2. IDENTITY CONFIGURATION
// ===============================================
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.Name = "CMCS.Auth";
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ===============================================
// 3. MVC AND CONTROLLERS CONFIGURATION
// ===============================================
builder.Services.AddControllersWithViews(options =>
{
    // Require authentication globally
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// ===============================================
// 4. AUTHORIZATION POLICIES
// ===============================================
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("RequireLecturerRole", policy =>
        policy.RequireRole("Lecturer"));

    options.AddPolicy("RequireCoordinatorRole", policy =>
        policy.RequireRole("ProgrammeCoordinator"));

    options.AddPolicy("RequireManagerRole", policy =>
        policy.RequireRole("AcademicManager"));

    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("SystemAdministrator"));

    // Combined policies
    options.AddPolicy("RequireApproverRole", policy =>
        policy.RequireRole("ProgrammeCoordinator", "AcademicManager"));

    options.AddPolicy("RequireAdminOrManagerRole", policy =>
        policy.RequireRole("SystemAdministrator", "AcademicManager"));

    // Claim-based policies
    options.AddPolicy("CanApproveClaims", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("ProgrammeCoordinator") ||
            context.User.IsInRole("AcademicManager")));

    options.AddPolicy("CanViewReports", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("ProgrammeCoordinator") ||
            context.User.IsInRole("AcademicManager") ||
            context.User.IsInRole("SystemAdministrator")));
});

// ===============================================
// 5. APPLICATION SERVICES REGISTRATION
// ===============================================

// Core business services
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IModuleService, ModuleService>();
builder.Services.AddScoped<IProgrammeService, ProgrammeService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, EnhancedNotificationService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<Contract_Monthly_Claim_System.Services.Implementation.IVerificationService, AutomatedVerificationService>();

// Additional services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// ===============================================
// 6. CONFIGURATION SETTINGS
// ===============================================
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<FileUploadSettings>(
    builder.Configuration.GetSection("FileUploadSettings"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// ===============================================
// 7. ADDITIONAL SERVICES
// ===============================================

// Memory caching
builder.Services.AddMemoryCache();

builder.Services.AddRazorPages();

// Session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "CMCS.Session";
});

// Anti-forgery token configuration
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "CMCS.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// JSON serialization options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// CORS (if needed for API endpoints)
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===============================================
// 8. LOGGING CONFIGURATION
// ===============================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsProduction())
{
    // Add additional logging providers for production
    // e.g., Application Insights, Serilog, etc.
}

// ===============================================
// 9. HEALTH CHECKS
// ===============================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddCheck("Email Service", () =>
    {
        // Add email service health check
        return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    });

// ===============================================
// 10. BUILD APPLICATION
// ===============================================
builder.Services.AddRazorPages();

var app = builder.Build();

// ===============================================
// 11. CONFIGURE HTTP REQUEST PIPELINE
// ===============================================

// Exception handling
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// HTTPS redirection
app.UseHttpsRedirection();

// Static files
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 30 days
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
    }
});

// Routing
app.UseRouting();

// CORS (if enabled)
// app.UseCors("DefaultPolicy");

// Session
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
// ===============================================
// 12. ROUTE CONFIGURATION
// ===============================================

// API routes
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller=Home}/{action=Index}/{id?}");

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages (for Identity)
app.MapRazorPages();

// Health checks endpoint
app.MapHealthChecks("/health");

// ===============================================
// 13. DATABASE INITIALIZATION
// ===============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Ensure database is created and seeded
        await SeedData.InitializeAsync(services);

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the database");

        // In production, you might want to handle this differently
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// ===============================================
// 14. RUN APPLICATION
// ===============================================
app.Run();