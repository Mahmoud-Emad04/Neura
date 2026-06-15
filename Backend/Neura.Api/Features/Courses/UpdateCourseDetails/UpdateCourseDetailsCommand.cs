using MediatR;

namespace Neura.Api.Features.Courses.UpdateCourseDetails;

public sealed record UpdateCourseDetailsCommand(string CourseIdKey, CourseUpdateRequest Request, string UserId)
    : IRequest<Result>;
