//using Neura.Core.Entities;

//namespace Neura.Repository.Persistence.EntitiesConfiguration;

//public class CourseRoleMaskConfiguration : IEntityTypeConfiguration<CourseRoleMask>
//{
//    public void Configure(EntityTypeBuilder<CourseRoleMask> builder)
//    {
//        var data = new List<CourseRoleMask>();

//        data.AddRange(
//                new CourseRoleMask
//                {
//                    Id = 1,
//                    RoleId = DefaultRoles.CourseOwnerRoleId,
//                    PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner]
//                }
//                ,
//                new CourseRoleMask
//                {
//                    Id = 2,
//                    RoleId = DefaultRoles.CoInstructorRoleId,
//                    PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CoInstructor]
//                },
//                new CourseRoleMask
//                {
//                    Id = 3,
//                    RoleId = DefaultRoles.TeachingAssistantRoleId,
//                    PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.TeachingAssistant]
//                },
//                new CourseRoleMask
//                {
//                    Id = 4,
//                    RoleId = DefaultRoles.StudentRoleId,
//                    PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.Student]
//                }
//        );

//        builder.HasData(data);

//    }
//}

