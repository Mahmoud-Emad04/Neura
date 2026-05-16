using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class AttemptViolationConfiguration : IEntityTypeConfiguration<AttemptViolation>
{
    public void Configure(EntityTypeBuilder<AttemptViolation> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.ViolationType)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.HasIndex(v => v.ExamAttemptId);
    }
}