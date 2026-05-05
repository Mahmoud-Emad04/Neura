using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Topic)
            .HasMaxLength(1_024);

        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>()    // Stored as "Text" / "Voice" — readable in DB
            .HasMaxLength(20);

        builder.Property(c => c.Position)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // ------------------------------------------------------------------
        // Relationships
        // ------------------------------------------------------------------

        builder.HasOne(c => c.Course)
            .WithMany(co => co.Channels)   // ⚠️ See note below — requires Course update
            .HasForeignKey(c => c.CourseId);

        // ------------------------------------------------------------------
        // Indexes
        // ------------------------------------------------------------------

        // ✅ Sidebar ordering index
        // Covers this exact query pattern:
        //   SELECT * FROM Channels
        //   WHERE CourseId = @courseId AND IsDeleted = 0
        //   ORDER BY Position ASC
        builder.HasIndex(c => new { c.CourseId, c.Position });

        // ------------------------------------------------------------------
        // Global Query Filter — soft delete
        // Use .IgnoreQueryFilters() in admin/moderation queries
        // ------------------------------------------------------------------

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}