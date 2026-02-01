using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.HasData(new ApplicationRole
        {
            Id = DefaultRoles.AdminRoleId,
            Name = DefaultRoles.Admin,
            NormalizedName = DefaultRoles.Admin.ToUpper(),
            ConcurrencyStamp = DefaultRoles.AdminRoleConcurrencyStamp
        }, new ApplicationRole
        {
            Id = DefaultRoles.MemberRoleId,
            Name = DefaultRoles.Member,
            IsDefualt = true,
            NormalizedName = DefaultRoles.Member.ToUpper(),
            ConcurrencyStamp = DefaultRoles.MemberRoleConcurrencyStamp
        });
    }
}