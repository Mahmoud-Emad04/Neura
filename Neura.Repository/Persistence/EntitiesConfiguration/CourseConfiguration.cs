using Neura.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        //builder.Property(c => c.Code).HasMaxLength(10);
        builder.Property(c => c.Title).HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(1000);
    }
}