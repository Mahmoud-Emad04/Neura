using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            logger.LogInformation("Starting database initialization...");

            // 1. Seed global roles (ASP.NET Identity)
            await SeedGlobalRolesAsync(roleManager, logger);

            // 2. Seed course roles
            await SeedCourseRolesAsync(context, logger);

            // 3. Seed default users
            await SeedDefaultUsersAsync(userManager, logger);

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private static async Task SeedGlobalRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        logger.LogInformation("Seeding global roles...");

        var roles = new List<(string Name, string Description, int Level)>
        {
            (DefaultRoles.SuperAdmin, "System owner with full control. Cannot be deleted or demoted.",
                (int)GlobalRole.SuperAdmin),
            (DefaultRoles.Admin, "Platform administrator. Can manage users, approve instructors, and moderate content.",
                (int)GlobalRole.Admin),
            (DefaultRoles.Instructor, "Verified content creator. Can create and manage courses.",
                (int)GlobalRole.Instructor),
            (DefaultRoles.Member, "Verified registered user. Can enroll in courses and apply to become instructor.",
                (int)GlobalRole.Member)
        };

        foreach (var (name, description, level) in roles)
        {
            var existingRole = await roleManager.FindByNameAsync(name);

            if (existingRole is null)
            {
                var role = new ApplicationRole
                {
                    Name = name,
                    Description = description,
                    Level = level,
                    IsDefault = true,
                    IsDeleted = false
                };

                var result = await roleManager.CreateAsync(role);

                if (result.Succeeded)
                    logger.LogInformation("Created global role: {RoleName}", name);
                else
                    logger.LogWarning("Failed to create global role {RoleName}: {Errors}",
                        name, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task SeedCourseRolesAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        logger.LogInformation("Seeding course roles...");

        // Check if already seeded
        if (await context.CourseRoles.AnyAsync())
        {
            logger.LogInformation("Course roles already exist, skipping seed");
            return; 
        }

        var courseRoles = new List<CourseRole>
        {
            new()
            {
                Name = nameof(CourseRoleType.Student),
                PermissionMask = CoursePermissionMasks.Student,
                Level = (int)CourseRoleType.Student,
                Description = "Can view course content only",
                IsSystem = true
            },
            new()
            {
                Name = nameof(CourseRoleType.Assistant),
                PermissionMask = CoursePermissionMasks.Assistant,
                Level = (int)CourseRoleType.Assistant,
                Description = "Can view analytics and help students with Q&A",
                IsSystem = true
            },
            new()
            {
                Name = nameof(CourseRoleType.CoInstructor),
                PermissionMask = CoursePermissionMasks.CoInstructor,
                Level = (int)CourseRoleType.CoInstructor,
                Description = "Can edit course content and view analytics",
                IsSystem = true
            },
            new()
            {
                Name = nameof(CourseRoleType.CourseOwner),
                PermissionMask = CoursePermissionMasks.CourseOwner,
                Level = (int)CourseRoleType.CourseOwner,
                Description = "Full control over the course including deletion and ownership transfer",
                IsSystem = true
            }
        };

        context.CourseRoles.AddRange(courseRoles);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} course roles", courseRoles.Count);
    }

    private static async Task SeedDefaultUsersAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        logger.LogInformation("Seeding default users...");

        // Seed SuperAdmin
        await SeedUserAsync(
            userManager,
            logger,
            "superadmin@neura.com",
            "SuperAdmin@123!",
            "Super",
            "Admin",
            [DefaultRoles.SuperAdmin]
        );

        // Seed Admin (optional - remove if not needed)
        await SeedUserAsync(
            userManager,
            logger,
            "admin@neura.com",
            "Admin@123!",
            "Platform",
            "Admin",
            [DefaultRoles.Admin]
        );

        // Seed Test Instructor (optional - for development/testing)
#if DEBUG
        await SeedUserAsync(
            userManager,
            logger,
            "instructor@neura.com",
            "Instructor@123!",
            "Test",
            "Instructor",
            [DefaultRoles.Instructor, DefaultRoles.Member],
            true
        );

        // Seed Test Member (optional - for development/testing)
        await SeedUserAsync(
            userManager,
            logger,
            "member@neura.com",
            "Member@123!",
            "Test",
            "Member",
            [DefaultRoles.Member]
        );
#endif
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string firstName,
        string lastName,
        string[] roles,
        bool isInstructor = false)
    {
        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            logger.LogInformation("User {Email} already exists, ensuring roles...", email);

            // Ensure user has all required roles
            foreach (var role in roles)
                if (!await userManager.IsInRoleAsync(existingUser, role))
                {
                    await userManager.AddToRoleAsync(existingUser, role);
                    logger.LogInformation("Added role {Role} to existing user {Email}", role, email);
                }

            return;
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true, // Pre-verified for seed users
            FirstName = firstName,
            LastName = lastName,
            Bio = $"Default {roles[0]} account",
            InstructorApprovedOn = isInstructor ? DateTime.UtcNow : null
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create user {Email}: {Errors}",
                email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Created user: {Email}", email);

        // Assign roles
        foreach (var role in roles)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (roleResult.Succeeded)
                logger.LogInformation("Assigned role {Role} to user {Email}", role, email);
            else
                logger.LogWarning("Failed to assign role {Role} to user {Email}: {Errors}",
                    role, email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}