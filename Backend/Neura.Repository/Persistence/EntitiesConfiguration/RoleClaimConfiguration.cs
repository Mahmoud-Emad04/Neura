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

        //int idCounter = roleClaims.Count() + 100;

        //foreach (var rolePerm in CourseRolePermissionMap.RolePermissions)
        //{
        //    var roleId = rolePerm.Key switch
        //    {
        //        DefaultRoles.CourseOwner => DefaultRoles.CourseOwnerRoleId,
        //        DefaultRoles.CoInstructor => DefaultRoles.CoInstructorRoleId,
        //        DefaultRoles.TeachingAssistant => DefaultRoles.TeachingAssistantRoleId,
        //        DefaultRoles.Student => DefaultRoles.StudentRoleId,
        //        _ => throw new Exception("Unknown role")
        //    };

        //    foreach (var perm in rolePerm.Value)
        //    {
        //        roleClaims.Add(new IdentityRoleClaim<string>
        //        {
        //            Id = idCounter++,
        //            RoleId = roleId,
        //            ClaimType = Permissions.Type,
        //            ClaimValue = perm
        //        });
        //    }
        //}

        builder.HasData(roleClaims);
    }
}
