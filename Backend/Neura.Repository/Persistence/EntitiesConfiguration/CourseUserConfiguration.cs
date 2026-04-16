using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseUserConfiguration : IEntityTypeConfiguration<CourseUser>
{
    public void Configure(EntityTypeBuilder<CourseUser> builder)
    {
        builder.ToTable("CourseUsers");

        // Composite primary key
        builder.HasKey(x => new { x.CourseId, x.UserId });

        builder.Property(x => x.UserId)
            .HasMaxLength(450); // Ensure consistent length

        builder.Property(x => x.PermissionMask)
            .IsRequired();

        builder.Property(x => x.EnrolledOn)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        // Relationships - Fix cascade delete issues
        builder.HasOne(x => x.Course)
            .WithMany(c => c.CourseUsers)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Keep cascade for course

        builder.HasOne(x => x.User)
            .WithMany(u => u.CourseUsers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        builder.HasOne(x => x.CourseRole)
            .WithMany(r => r.CourseUsers)
            .HasForeignKey(x => x.CourseRoleId)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        builder.HasOne(x => x.EnrolledBy)
            .WithMany()
            .HasForeignKey(x => x.EnrolledById)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        builder.HasOne(x => x.Invitation)
            .WithMany()
            .HasForeignKey(x => x.InvitationId)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CourseRoleId);
        builder.HasIndex(x => new { x.CourseId, x.IsDeleted });

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}