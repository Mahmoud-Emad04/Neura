using MediatR;

namespace Neura.Api.Features.Courses.GetCourseContent;

public sealed record GetCourseContentQuery(string CourseIdKey, string? UserId)
    : IRequest<Result<CourseResponse>>;
