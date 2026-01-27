using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.Property(c => c.ImageUrl).HasMaxLength(100);
        builder.Property(c => c.Title).HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}