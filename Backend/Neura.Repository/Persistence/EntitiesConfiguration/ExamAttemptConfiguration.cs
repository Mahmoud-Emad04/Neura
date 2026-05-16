using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(a => a.Score)
               .HasPrecision(7, 2);

        builder.Property(a => a.ScorePercentage)
               .HasPrecision(5, 2);

        // JSON columns for shuffle order storage
        builder.Property(a => a.QuestionOrder)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.Property(a => a.AnswerOrder)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId);

        builder.HasMany(a => a.AttemptAnswers)
               .WithOne(aa => aa.ExamAttempt)
               .HasForeignKey(aa => aa.ExamAttemptId);

        builder.HasMany(a => a.Violations)
               .WithOne(v => v.ExamAttempt)
               .HasForeignKey(v => v.ExamAttemptId);

        // Index: fast lookup — "how many attempts does this user have for this exam?"
        builder.HasIndex(a => new { a.ExamId, a.UserId });

        // Index: background job needs to find timed-out attempts fast
        builder.HasIndex(a => new { a.Status, a.StartedAt })
               .HasFilter("[Status] = 'InProgress'");
    }
}