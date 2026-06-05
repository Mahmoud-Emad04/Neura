using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Neura.Core.Entities;
using System.Reflection;
using System.Security.Claims;

namespace Neura.Repository.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IHttpContextAccessor httpContextAccessor
) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public DbSet<Tag> Tags { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Channel> Channels { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<CourseUser> CourseUsers { get; set; }
    public DbSet<CourseRole> CourseRoles { get; set; }
    public DbSet<PostComment> PostComments { get; set; }
    public DbSet<ExamAttempt> ExamAttempts { get; set; }
    public DbSet<AnswerOption> AnswerOptions { get; set; }
    public DbSet<AttemptAnswer> AttemptAnswers { get; set; }
    public DbSet<ExamViolation> ExamViolations { get; set; }
    public DbSet<CourseBookmark> CourseBookmarks { get; set; }
    public DbSet<AttemptViolation> AttemptViolations { get; set; }
    public DbSet<CourseInvitation> CourseInvitations { get; set; }
    public DbSet<LessonCompletion> LessonCompletions { get; set; }
    public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
    public DbSet<AttemptAnswerOption> AttemptAnswerOptions { get; set; }
    public DbSet<CourseLearningOutcome> CourseLearningOutcomes { get; set; }
    public DbSet<InstructorApplication> InstructorApplications { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }
    public DbSet<Payment> Payments { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var cascadFks = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

        foreach (var fk in cascadFks)
        {
            if (fk.DeclaringEntityType.ClrType == typeof(CourseUser))
                continue;
            if (fk.PrincipalEntityType.ClrType == typeof(Post))
                continue;
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Lesson>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Exam>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Question>().HasQueryFilter(a => !a.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        // HttpContext is null for SignalR WebSocket transport (after the initial handshake).
        // Gracefully skip audit fields when userId cannot be resolved.
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entityEntry in entries)
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedOn = DateTime.UtcNow;
                if (userId is not null)
                    entityEntry.Entity.CreatedById = userId;
            }
            else
            {
                entityEntry.Entity.UpdatedOn = DateTime.UtcNow;
                if (userId is not null)
                    entityEntry.Entity.UpdatedById = userId;
            }

        return base.SaveChangesAsync(cancellationToken);
    }
}