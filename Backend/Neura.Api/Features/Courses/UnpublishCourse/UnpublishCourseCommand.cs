using MediatR;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.UnpublishCourse;

public sealed record UnpublishCourseCommand(string CourseIdKey, string UserId)
    : IRequest<Result<CourseStatusUpdateResponse>>;
