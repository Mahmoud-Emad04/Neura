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
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<PostLike> PostLikes { get; set; }
    public DbSet<CourseUser> CourseUsers { get; set; }
    public DbSet<PostComment> PostComments { get; set; }
    public DbSet<CourseBookmark> CourseBookmarks { get; set; }
    public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
    public DbSet<CourseLearningOutcome> CourseLearningOutcomes { get; set; }
    public DbSet<UploadedFile> UploadedFiles { get; set; }


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
            // Allow Cascade if the PARENT being deleted is a Post
            // (This covers both Post -> Comments and Post -> Likes)
            if (fk.PrincipalEntityType.ClrType == typeof(Post))
                continue;
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }


        //modelBuilder.Entity<Answer>().HasQueryFilter(a => a.IsActive);

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        foreach (var entityEntry in entries)
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedOn = DateTime.UtcNow;
                entityEntry.Entity.CreatedById = userId;
            }
            else
            {
                entityEntry.Entity.UpdatedOn = DateTime.UtcNow;
                entityEntry.Entity.UpdatedById = userId;
            }

        return base.SaveChangesAsync(cancellationToken);
    }
}