using Microsoft.AspNetCore.Http;
using GraduationProject.Core.Abstractions;

namespace GraduationProject.Core.Errors;

public static class CourseErrors
{
    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified Course was not found.", StatusCodes.Status404NotFound);

}