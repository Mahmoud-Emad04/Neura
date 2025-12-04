using Microsoft.AspNetCore.Http;
using Neura.Core.Abstractions;
using Neura.Core.Entities;

namespace Neura.Core.Errors;

public static class CourseErrors
{
    public static readonly Error CourseNotFound =
        new("Course.NotFound", "The specified Course was not found.", StatusCodes.Status404NotFound);
    public static readonly Error CourseTagNotFound =
        new ("Course.TagNotFound", "One or more provided tag IDs do not exist.", StatusCodes.Status404NotFound);

}