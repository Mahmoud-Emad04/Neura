namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseUserConfiguration : IEntityTypeConfiguration<CourseUser>
{
    public void Configure(EntityTypeBuilder<CourseUser> builder)
    {
        builder.HasKey(cu => new { cu.CourseId, cu.UserId });
        builder.HasQueryFilter(cu => !cu.IsDeleted && !cu.Course.IsDeleted);
    }
}