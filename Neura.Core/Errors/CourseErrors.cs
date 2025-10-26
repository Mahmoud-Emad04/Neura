using Microsoft.AspNetCore.Http;
using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class CourseErrors
{
    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified Course was not found.", StatusCodes.Status404NotFound);

}