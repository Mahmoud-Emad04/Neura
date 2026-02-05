using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(250);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.HasQueryFilter(c => !c.IsDeleted);
        builder.Property(s => s.Position)
            .IsRequired();

        builder.Property(s => s.IsDeleted)
            .HasDefaultValue(false);


        // Section -> Course (required)
        //builder.HasOne(s => s.Course)
        //	.WithMany()
        //	.HasForeignKey(s => s.CourseId)
        //	.OnDelete(DeleteBehavior.Restrict)
        //	.IsRequired();

        // TO BE reviewed:
        // Section -> Lessons (dependent). Lesson.SectionId is optional in the model, so configure FK accordingly.
        //builder.HasMany(s => s.Lessons)
        //	.WithOne(l => l.Section)
        //	.HasForeignKey(l => l.SectionId)
        //	.OnDelete(DeleteBehavior.Restrict);
    }
}