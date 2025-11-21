using Contract_Monthly_Claim_System.Data.CMCS.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Contract_Monthly_Claim_System.Data
{
    namespace CMCS.Data
    {
        public class ApplicationDbContext : IdentityDbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }

            // DbSets for all entities
            public DbSet<User> Users { get; set; }
            public DbSet<Lecturer> Lecturers { get; set; }
            public DbSet<ProgrammeCoordinator> ProgrammeCoordinators { get; set; }
            public DbSet<AcademicManager> AcademicManagers { get; set; }
            public DbSet<Programme> Programmes { get; set; }
            public DbSet<Module> Modules { get; set; }
            public DbSet<Claim> Claims { get; set; }
            public DbSet<ClaimItem> ClaimItems { get; set; }
            public DbSet<Document> Documents { get; set; }
            public DbSet<ClaimStatusHistory> ClaimStatusHistories { get; set; }
            public DbSet<LecturerModule> LecturerModules { get; set; }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                base.OnModelCreating(builder);


                builder.Entity<ClaimItem>()
                .Property(c => c.HoursWorked)
                .HasColumnType("decimal(18,2)");

                builder.Entity<ClaimItem>()
                    .Property(c => c.HourlyRate)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<Claim>()
                    .Property(c => c.TotalHours)
                    .HasColumnType("decimal(18,2)");

                // Configure User inheritance (TPH - Table Per Hierarchy)
                builder.Entity<User>()
                  .HasDiscriminator<UserType>(u => u.UserType)
                  .HasValue<Lecturer>(UserType.Lecturer)
                  .HasValue<ProgrammeCoordinator>(UserType.ProgrammeCoordinator)
                  .HasValue<AcademicManager>(UserType.AcademicManager);

                // Configure LecturerModule junction table
                builder.Entity<LecturerModule>()
                    .HasKey(lm => new { lm.LecturerId, lm.ModuleId });

                builder.Entity<LecturerModule>()
                    .HasOne(lm => lm.Lecturer)
                    .WithMany(l => l.LecturerModules)
                    .HasForeignKey(lm => lm.LecturerId);

                builder.Entity<LecturerModule>()
                    .HasOne(lm => lm.Module)
                    .WithMany(m => m.LecturerModules)
                    .HasForeignKey(lm => lm.ModuleId);

                // Configure Claim relationships
                builder.Entity<Claim>()
                    .HasOne(c => c.Lecturer)
                    .WithMany(l => l.Claims)
                    .HasForeignKey(c => c.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);

                builder.Entity<Claim>()
                    .HasOne(c => c.Coordinator)
                    .WithMany(pc => pc.CoordinatorApprovedClaims)
                    .HasForeignKey(c => c.CoordinatorId)
                    .OnDelete(DeleteBehavior.NoAction)  // Changed from SetNull
                    .IsRequired(false);

                builder.Entity<Claim>()
                    .HasOne(c => c.Manager)
                    .WithMany(am => am.ManagerApprovedClaims)
                    .HasForeignKey(c => c.ManagerId)
                    .OnDelete(DeleteBehavior.NoAction)  // Changed from SetNull
                    .IsRequired(false);

                // Configure ClaimItem relationships
                builder.Entity<ClaimItem>()
                    .HasOne(ci => ci.Claim)
                    .WithMany(c => c.ClaimItems)
                    .HasForeignKey(ci => ci.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Entity<ClaimItem>()
                    .HasOne(ci => ci.Module)
                    .WithMany(m => m.ClaimItems)
                    .HasForeignKey(ci => ci.ModuleId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Document relationships
                builder.Entity<Document>()
                    .HasOne(d => d.Claim)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure Programme relationships
                builder.Entity<Programme>()
                    .HasOne(p => p.Coordinator)
                    .WithMany(pc => pc.ManagedProgrammes)
                    .HasForeignKey(p => p.CoordinatorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Module relationships
                builder.Entity<Module>()
                    .HasOne(m => m.Programme)
                    .WithMany(p => p.Modules)
                    .HasForeignKey(m => m.ProgrammeId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure ClaimStatusHistory relationships
                builder.Entity<ClaimStatusHistory>()
                    .HasKey(csh => csh.StatusHistoryId);

                builder.Entity<ClaimStatusHistory>()
                    .HasOne(csh => csh.Claim)
                    .WithMany(c => c.StatusHistory)
                    .HasForeignKey(csh => csh.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.Entity<ClaimStatusHistory>()
                    .HasOne(csh => csh.ChangedBy)
                    .WithMany()
                    .HasForeignKey(csh => csh.ChangedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Configure decimal precision
                builder.Entity<Claim>()
                    .Property(c => c.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<ClaimItem>()
                    .Property(ci => ci.HourlyRate)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<ClaimItem>()
                    .Property(ci => ci.TotalAmount)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<Module>()
                    .Property(m => m.HourlyRate)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<Lecturer>()
                    .Property(l => l.DefaultHourlyRate)
                    .HasColumnType("decimal(18,2)");

                builder.Entity<AcademicManager>()
                    .Property(am => am.ApprovalLimit)
                    .HasColumnType("decimal(18,2)");

                // Configure string lengths
                builder.Entity<User>()
                    .Property(u => u.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                builder.Entity<User>()
                    .Property(u => u.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                builder.Entity<User>()
                    .Property(u => u.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                builder.Entity<Claim>()
                    .Property(c => c.ClaimNumber)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Entity<Programme>()
                    .Property(p => p.ProgrammeCode)
                    .HasMaxLength(20)
                    .IsRequired();

                builder.Entity<Module>()
                    .Property(m => m.ModuleCode)
                    .HasMaxLength(20)
                    .IsRequired();

                // Configure indexes
                builder.Entity<Claim>()
                    .HasIndex(c => c.ClaimNumber)
                    .IsUnique();

                builder.Entity<Programme>()
                    .HasIndex(p => p.ProgrammeCode)
                    .IsUnique();

                builder.Entity<Module>()
                    .HasIndex(m => m.ModuleCode)
                    .IsUnique();

                builder.Entity<User>()
                    .HasIndex(u => u.Email)
                    .IsUnique();

                // Seed initial data
               // SeedData(builder);
            }

            private void SeedData(ModelBuilder builder)
            {
                // Seed UserTypes (if using enum table approach)
                // Seed initial programmes
                builder.Entity<Programme>().HasData(
                    new Programme
                    {
                        ProgrammeId = 1,
                        ProgrammeName = "Bachelor of Commerce in Accounting",
                        ProgrammeCode = "BCOM-ACC",
                        Description = "Comprehensive accounting degree program",
                        CoordinatorId = 1,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Programme
                    {
                        ProgrammeId = 2,
                        ProgrammeName = "Bachelor of Science in Computer Science",
                        ProgrammeCode = "BSC-CS",
                        Description = "Computer science degree program",
                        CoordinatorId = 2,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Programme
                    {
                        ProgrammeId = 3,
                        ProgrammeName = "Bachelor of Arts in Psychology",
                        ProgrammeCode = "BA-PSY",
                        Description = "Psychology degree program",
                        CoordinatorId = 1,
                        CreatedDate = DateTime.UtcNow
                    }
                );

                // Seed initial modules
                builder.Entity<Module>().HasData(
                    new Module
                    {
                        ModuleId = 1,
                        ModuleName = "Financial Accounting I",
                        ModuleCode = "ACC101",
                        Description = "Introduction to financial accounting principles",
                        ProgrammeId = 1,
                        HourlyRate = 450.00m,
                        CreditHours = 120,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Module
                    {
                        ModuleId = 2,
                        ModuleName = "Management Accounting",
                        ModuleCode = "ACC201",
                        Description = "Management accounting concepts and applications",
                        ProgrammeId = 1,
                        HourlyRate = 500.00m,
                        CreditHours = 120,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Module
                    {
                        ModuleId = 3,
                        ModuleName = "Data Structures and Algorithms",
                        ModuleCode = "CS201",
                        Description = "Advanced data structures and algorithm design",
                        ProgrammeId = 2,
                        HourlyRate = 550.00m,
                        CreditHours = 150,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Module
                    {
                        ModuleId = 4,
                        ModuleName = "Database Management Systems",
                        ModuleCode = "CS301",
                        Description = "Design and implementation of database systems",
                        ProgrammeId = 2,
                        HourlyRate = 600.00m,
                        CreditHours = 150,
                        CreatedDate = DateTime.UtcNow
                    },
                    new Module
                    {
                        ModuleId = 5,
                        ModuleName = "Introduction to Psychology",
                        ModuleCode = "PSY101",
                        Description = "Fundamental concepts in psychology",
                        ProgrammeId = 3,
                        HourlyRate = 400.00m,
                        CreditHours = 120,
                        CreatedDate = DateTime.UtcNow
                    }
                );
            }
        }

    }

    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.MigrateAsync();

            // 1. Seed roles FIRST
            await SeedRolesAsync(roleManager);

            // 2. Seed users SECOND
            await SeedUsersAsync(userManager, context);

            // 3. Seed programmes THIRD (after coordinators exist)
            await SeedProgrammesAsync(context);

            // 4. Seed modules FOURTH
            await SeedModulesAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = new[] { "Lecturer", "ProgrammeCoordinator", "AcademicManager", "SystemAdministrator" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            // Seed Programme Coordinator FIRST (before programmes)
            if (await userManager.FindByEmailAsync("coordinator@university.edu") == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = "coordinator@university.edu",
                    Email = "coordinator@university.edu",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, "Coordinator123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, "ProgrammeCoordinator");

                    var coordinator = new ProgrammeCoordinator
                    {
                        FirstName = "Jane",
                        LastName = "Doe",
                        Email = "coordinator@university.edu",
                        PhoneNumber = "011-555-0124",
                        Department = "Business Studies",
                        IdentityUserId = identityUser.Id,
                        UserType = UserType.ProgrammeCoordinator,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    context.ProgrammeCoordinators.Add(coordinator);
                    await context.SaveChangesAsync(); // IMPORTANT: Save to get the UserId
                }
            }

            // Seed Lecturer
            if (await userManager.FindByEmailAsync("lecturer@university.edu") == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = "lecturer@university.edu",
                    Email = "lecturer@university.edu",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, "Lecturer123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, "Lecturer");

                    var lecturer = new Lecturer
                    {
                        FirstName = "John",
                        LastName = "Smith",
                        Email = "lecturer@university.edu",
                        PhoneNumber = "011-555-0123",
                        EmployeeNumber = "EMP001",
                        Specialization = "Accounting",
                        DefaultHourlyRate = 450.00m,
                        BankAccountNumber = "1234567890",
                        TaxNumber = "9876543210",
                        IdentityUserId = identityUser.Id,
                        UserType = UserType.Lecturer,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    context.Lecturers.Add(lecturer);
                    await context.SaveChangesAsync();
                }
            }

            // Seed Academic Manager
            if (await userManager.FindByEmailAsync("manager@university.edu") == null)
            {
                var identityUser = new IdentityUser
                {
                    UserName = "manager@university.edu",
                    Email = "manager@university.edu",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(identityUser, "Manager123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(identityUser, "AcademicManager");

                    var manager = new AcademicManager
                    {
                        FirstName = "Michael",
                        LastName = "Johnson",
                        Email = "manager@university.edu",
                        PhoneNumber = "011-555-0125",
                        Division = "Academic Affairs",
                        ApprovalLimit = 100000.00m,
                        IdentityUserId = identityUser.Id,
                        UserType = UserType.AcademicManager,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    context.AcademicManagers.Add(manager);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedProgrammesAsync(ApplicationDbContext context)
        {
            if (await context.Programmes.AnyAsync())
                return; // Already seeded

            // Get the coordinator we created
            var coordinator = await context.ProgrammeCoordinators.FirstOrDefaultAsync();
            if (coordinator == null)
                return; // Can't seed without coordinator

            var programmes = new[]
            {
            new Programme
            {
                ProgrammeName = "Bachelor of Commerce in Accounting",
                ProgrammeCode = "BCOM-ACC",
                Description = "Comprehensive accounting degree program",
                CoordinatorId = coordinator.UserId, // Use the actual coordinator ID
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            },
            new Programme
            {
                ProgrammeName = "Bachelor of Science in Computer Science",
                ProgrammeCode = "BSC-CS",
                Description = "Computer science degree program",
                CoordinatorId = coordinator.UserId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            },
            new Programme
            {
                ProgrammeName = "Bachelor of Arts in Psychology",
                ProgrammeCode = "BA-PSY",
                Description = "Psychology degree program",
                CoordinatorId = coordinator.UserId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            }
        };

            context.Programmes.AddRange(programmes);
            await context.SaveChangesAsync();
        }

        private static async Task SeedModulesAsync(ApplicationDbContext context)
        {
            if (await context.Modules.AnyAsync())
                return; // Already seeded

            var programmes = await context.Programmes.ToListAsync();
            if (!programmes.Any())
                return; // Can't seed without programmes

            var modules = new[]
            {
            new Module
            {
                ModuleName = "Financial Accounting I",
                ModuleCode = "ACC101",
                Description = "Introduction to financial accounting principles",
                ProgrammeId = programmes[0].ProgrammeId,
                HourlyRate = 450.00m,
                CreditHours = 120,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            },
            new Module
            {
                ModuleName = "Management Accounting",
                ModuleCode = "ACC201",
                Description = "Management accounting concepts",
                ProgrammeId = programmes[0].ProgrammeId,
                HourlyRate = 500.00m,
                CreditHours = 120,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            },
            new Module
            {
                ModuleName = "Data Structures and Algorithms",
                ModuleCode = "CS201",
                Description = "Advanced data structures",
                ProgrammeId = programmes[1].ProgrammeId,
                HourlyRate = 550.00m,
                CreditHours = 150,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            }
        };

            context.Modules.AddRange(modules);
            await context.SaveChangesAsync();
        }
    }
}




