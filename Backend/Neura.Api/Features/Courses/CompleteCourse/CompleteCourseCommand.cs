using MediatR;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.CompleteCourse;

public sealed record CompleteCourseCommand(string CourseIdKey, string UserId)
    : IRequest<Result<CourseStatusUpdateResponse>>;
