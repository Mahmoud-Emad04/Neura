using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseRoleConfiguration : IEntityTypeConfiguration<CourseRole>
{
    public void Configure(EntityTypeBuilder<CourseRole> builder)
    {
        builder.ToTable("CourseRoles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.PermissionMask)
            .IsRequired();

        builder.Property(x => x.Level)
            .IsRequired();

        builder.Property(x => x.IsSystem)
            .HasDefaultValue(true);

        // Unique constraint on Name
        builder.HasIndex(x => x.Name)
            .IsUnique();

        // Index on Level for hierarchy queries
        builder.HasIndex(x => x.Level);
    }
}