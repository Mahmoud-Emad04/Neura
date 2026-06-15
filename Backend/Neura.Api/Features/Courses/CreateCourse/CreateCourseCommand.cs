using MediatR;

namespace Neura.Api.Features.Courses.CreateCourse;

public sealed record CreateCourseCommand(CourseRequest Request, string UserId)
    : IRequest<Result<CourseMetadataResponse>>;
