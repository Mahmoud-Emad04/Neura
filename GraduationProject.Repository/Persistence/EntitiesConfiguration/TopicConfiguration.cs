using GraduationProject.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GraduationProject.Repository.Persistence.EntitiesConfiguration;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasIndex(p => new { p.Name, p.CourseId }).IsUnique();
    }
}