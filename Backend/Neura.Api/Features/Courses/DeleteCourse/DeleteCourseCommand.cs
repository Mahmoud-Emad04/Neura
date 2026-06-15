using MediatR;

namespace Neura.Api.Features.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(string CourseIdKey, string UserId)
    : IRequest<Result>;
