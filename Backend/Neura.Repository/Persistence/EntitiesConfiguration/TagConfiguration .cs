using Neura.Core.Entities;

namespace Neura.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.IconUrl)
            .HasMaxLength(500);

        builder.Property(t => t.ColorHex)
            .HasMaxLength(7); // #FFFFFF

        // Indexes
        builder.HasIndex(t => t.Name)
            .IsUnique();

        builder.HasIndex(t => t.IsActive);


        builder.HasIndex(t => new { t.IsActive, t.IsDeleted });

        // Global query filter for soft delete
        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasMany(t => t.Courses)
            .WithMany(c => c.Tags)
            .UsingEntity<Dictionary<string, object>>(
                "CourseTag",
                j => j.HasOne<Course>().WithMany().HasForeignKey("CourseId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade));
    }
}