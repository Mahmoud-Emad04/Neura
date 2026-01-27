using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasIndex(p => new { p.Name, p.CourseId }).IsUnique();
        builder.HasQueryFilter(t => !t.Course.IsDeleted);
    }
}