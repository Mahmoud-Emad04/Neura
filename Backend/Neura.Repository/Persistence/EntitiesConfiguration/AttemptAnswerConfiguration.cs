using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;


public class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => new { a.ExamAttemptId, a.QuestionId }).IsUnique();

        builder.HasMany(a => a.SelectedOptions)
               .WithOne(so => so.AttemptAnswer)
               .HasForeignKey(so => so.AttemptAnswerId);
    }
}