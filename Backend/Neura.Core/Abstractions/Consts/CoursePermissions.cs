namespace Neura.Core.Abstractions.Consts;

[Flags]
public enum CoursePermission
{
    None = 0,

    ViewPublicDetails = 1 << 0,

    AccessContent = 1 << 1,

    SubmitAssignment = 1 << 2,

    GradeAssignment = 1 << 3,

    UpdateCourse = 1 << 4,

    DeleteCourse = 1 << 5
}