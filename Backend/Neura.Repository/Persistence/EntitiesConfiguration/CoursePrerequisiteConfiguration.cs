using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {

        builder.HasKey(x => new { x.CourseId, x.Requirement });

        builder.Property(x => x.Requirement)
            .HasMaxLength(200)
            .IsRequired();
    }
}
