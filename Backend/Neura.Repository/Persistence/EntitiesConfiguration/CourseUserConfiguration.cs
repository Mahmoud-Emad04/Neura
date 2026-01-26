namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseUserConfiguration : IEntityTypeConfiguration<CourseUser>
{
    public void Configure(EntityTypeBuilder<CourseUser> builder)
    {
        builder.HasKey(cu => new { cu.CourseId, cu.UserId });
        // TODO Complate 
    }
}