namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<string>> builder)
    {
        var permissions = Permissions.GetAll();
        var roleClaims = new List<IdentityRoleClaim<string>>();

        for (int i = 0; i < permissions.Count; i++)
        {
            roleClaims.Add(new IdentityRoleClaim<string>
            {
                Id = i + 1,
                RoleId = DefaultRoles.AdminRoleId,
                ClaimType = Permissions.Type,
                ClaimValue = permissions[i]
            });
        }

        builder.HasData(roleClaims);
    }
}
