using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.LessonId).IsUnique();

        builder.HasOne(e => e.Lesson)
               .WithOne(l => l.Exam)
               .HasForeignKey<Exam>(e => e.LessonId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Title)
               .IsRequired()
               .HasMaxLength(300);

        builder.Property(e => e.Description)
               .HasMaxLength(2000);

        builder.Property(e => e.PassingScorePercentage)
               .HasPrecision(5, 2);

        builder.Property(e => e.ShowCorrectAnswersAfterSubmit)
               .HasDefaultValue(true);

        builder.Property(e => e.IsPublished)
               .HasDefaultValue(false);

        builder.Property(e => e.AreGradesPublished)
               .HasDefaultValue(false);

        builder.HasMany(e => e.Questions)
               .WithOne(q => q.Exam)
               .HasForeignKey(q => q.ExamId);

        builder.HasMany(e => e.Attempts)
               .WithOne(a => a.Exam)
               .HasForeignKey(a => a.ExamId);
    }
}