using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsDefault)
            .HasDefaultValue(false);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.Level)
            .HasDefaultValue(0);

        // Index for hierarchy queries
        builder.HasIndex(x => x.Level);
    }
}