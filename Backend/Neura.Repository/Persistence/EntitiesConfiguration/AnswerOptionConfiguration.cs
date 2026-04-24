using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text)
               .IsRequired()
               .HasMaxLength(1000);

        builder.HasIndex(a => new { a.QuestionId, a.Order });
    }
}