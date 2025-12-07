namespace Neura.Core.Abstractions.Consts;

[Flags]
public enum CoursePermission : int
{
    ViewCourse = 1 << 0,
    UpdateCourse = 1 << 1,
    DeleteCourse = 1 << 2
}

