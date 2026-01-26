using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseUser> CourseUsers { get; set; }
    public DbSet<CourseRoleMask> CourseRoleMasks { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Tag> Tags { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global query filter to exclude soft-deleted courses
        modelBuilder.Entity<Course>().HasQueryFilter(c => !c.IsDeleted);

        var cascadFks = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

        foreach (var fk in cascadFks)
            fk.DeleteBehavior = DeleteBehavior.Restrict;

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