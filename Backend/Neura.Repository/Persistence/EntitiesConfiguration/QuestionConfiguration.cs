using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText)
               .IsRequired();

        builder.Property(q => q.Points)
               .HasPrecision(5, 2);

        builder.Property(q => q.QuestionType)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(q => q.Level)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(QuestionLevel.Easy);

        builder.HasMany(q => q.AnswerOptions)
               .WithOne(a => a.Question)
               .HasForeignKey(a => a.QuestionId);

        builder.HasMany(q => q.AttemptAnswers)
               .WithOne(aa => aa.Question)
               .HasForeignKey(aa => aa.QuestionId);

        builder.HasIndex(q => new { q.ExamId, q.Order });
    }
}