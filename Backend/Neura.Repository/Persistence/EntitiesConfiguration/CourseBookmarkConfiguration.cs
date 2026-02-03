namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseBookmarkConfiguration : IEntityTypeConfiguration<CourseBookmark>
{
    public void Configure(EntityTypeBuilder<CourseBookmark> builder)
    {
        builder.HasKey(cb => new { cb.UserId, cb.CourseId });
    }
}