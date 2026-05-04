using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class LessonCompletionConfiguration : IEntityTypeConfiguration<LessonCompletion>
{
    public void Configure(EntityTypeBuilder<LessonCompletion> builder)
    {
        builder.HasKey(lc => new { lc.UserId, lc.LessonId });

        builder.HasOne(lc => lc.User)
            .WithMany()
            .HasForeignKey(lc => lc.UserId);

        builder.HasOne(lc => lc.Lesson)
            .WithMany(l => l.Completions)
            .HasForeignKey(lc => lc.LessonId);

        builder.HasIndex(lc => lc.LessonId);
        builder.HasIndex(lc => lc.UserId);
    }
}