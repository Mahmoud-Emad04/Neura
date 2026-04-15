using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(4000);

        builder.Property(c => c.DisplayInstructorName)
            .HasMaxLength(100);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(CourseStatus.Pending);

        builder.Property(c => c.Rating);
        builder.Property(c => c.TotalRatingSum);
        builder.Property(c => c.TotalReviews);

        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedById);
        builder.HasIndex(c => new { c.Status, c.IsDeleted });
    }
}