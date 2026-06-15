using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Type).HasConversion<byte>();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.VideoText)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.VideoProcessingStatus)
            .HasConversion<byte>()
            .HasDefaultValue(VideoProcessingStatus.None);

        //builder.Property(x => x.VideoUrl)
        //    .HasMaxLength(500);

        builder.HasIndex(x => new { x.SectionId, x.OrderIndex });
    }
}