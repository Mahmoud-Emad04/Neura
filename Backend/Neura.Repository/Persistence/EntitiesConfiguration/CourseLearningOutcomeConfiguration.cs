using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseLearningOutcomeConfiguration : IEntityTypeConfiguration<CourseLearningOutcome>
{
    public void Configure(EntityTypeBuilder<CourseLearningOutcome> builder)
    {
        builder.HasKey(x => new { x.CourseId, x.Outcome });

        builder.Property(x => x.Outcome)
            .HasMaxLength(200)
            .IsRequired();
    }
}