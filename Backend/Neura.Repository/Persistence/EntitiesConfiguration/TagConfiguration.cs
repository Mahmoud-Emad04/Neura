using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(100);
    }
}