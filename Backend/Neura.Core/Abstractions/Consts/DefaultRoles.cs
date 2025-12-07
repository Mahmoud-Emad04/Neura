namespace Neura.Core.Abstractions.Consts;

public static class DefaultRoles
{
    // Global System Roles

    public const string Admin = nameof(Admin);
    public const string AdminRoleId = "019a1c20-390e-7fd8-9b20-cddc38906b5b";
    public const string AdminRoleConcurrencyStamp = "019a1c20-390e-7fd8-9b20-cdddf127ba16";

    public const string Member = nameof(Member);
    public const string MemberRoleId = "019a1c20-390e-7fd8-9b20-cde0cc78e33e";
    public const string MemberRoleConcurrencyStamp = "019a1c20-390e-7fd8-9b20-cddf89d2a037";

    // Course-Scoped Roles

    public const string CourseOwner = nameof(CourseOwner);
    public const string CourseOwnerRoleId = "019aeef9-ea10-7594-a042-ebc8958f1366";
    public const string CourseOwnerConcurrencyStamp = "019aeef9-ea10-7594-a042-ebc9c5b329bd";

    public const string CoInstructor = nameof(CoInstructor);
    public const string CoInstructorRoleId = "019aeef9-ea10-7594-a042-ebca472ee63f";
    public const string CoInstructorConcurrencyStamp = "019aeef9-ea10-7594-a042-ebcbaa20d23d";

    public const string TeachingAssistant = nameof(TeachingAssistant);
    public const string TeachingAssistantRoleId = "019aeef9-ea10-7594-a042-ebccf58eb683";
    public const string TeachingAssistantConcurrencyStamp = "019aeef9-ea10-7594-a042-ebcdfbd1c0ff";

    public const string Student = nameof(Student);
    public const string StudentRoleId = "019aeef9-ea10-7594-a042-ebce4c1dec9b";
    public const string StudentConcurrencyStamp = "019aeef9-ea10-7594-a042-ebcf7ddf3644";
}
