namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(k => new { k.UserId, k.CourseId });

        //builder.HasOne(r => r.Course)
        //    .WithMany(c => c.Reviews)
        //    .HasForeignKey(r => r.CourseId);
    }
}