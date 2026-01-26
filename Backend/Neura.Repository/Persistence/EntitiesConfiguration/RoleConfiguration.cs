using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class RoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.HasData([
            new ApplicationRole{
                Id = DefaultRoles.AdminRoleId,
                Name = DefaultRoles.Admin,
                NormalizedName = DefaultRoles.Admin.ToUpper(),
                ConcurrencyStamp = DefaultRoles.AdminRoleConcurrencyStamp
        },
            new ApplicationRole{
                Id = DefaultRoles.MemberRoleId,
                Name = DefaultRoles.Member,
                IsDefualt = true,
                NormalizedName = DefaultRoles.Member.ToUpper(),
                ConcurrencyStamp = DefaultRoles.MemberRoleConcurrencyStamp
        }
        //    new ApplicationRole{
        //        Id = DefaultRoles.CourseOwnerRoleId,
        //        Name = DefaultRoles.CourseOwner,
        //        NormalizedName = DefaultRoles.CourseOwner.ToUpper(),
        //        ConcurrencyStamp = DefaultRoles.CourseOwnerConcurrencyStamp
        // },
        //    new ApplicationRole{
        //        Id = DefaultRoles.CoInstructorRoleId,
        //        Name = DefaultRoles.CoInstructor,
        //        NormalizedName = DefaultRoles.CoInstructor.ToUpper(),
        //        ConcurrencyStamp = DefaultRoles.CoInstructorConcurrencyStamp
        //},
        //    new ApplicationRole{
        //        Id = DefaultRoles.TeachingAssistantRoleId,
        //        Name = DefaultRoles.TeachingAssistant,
        //        NormalizedName = DefaultRoles.TeachingAssistant.ToUpper(),
        //        ConcurrencyStamp = DefaultRoles.TeachingAssistantConcurrencyStamp
        //},
        //    new ApplicationRole{
        //        Id = DefaultRoles.StudentRoleId,
        //        Name = DefaultRoles.Student,
        //        NormalizedName = DefaultRoles.Student.ToUpper(),
        //        ConcurrencyStamp = DefaultRoles.StudentConcurrencyStamp
        //}
        ]);
    }
}