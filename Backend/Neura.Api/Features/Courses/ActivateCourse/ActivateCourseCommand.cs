using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.ActivateCourse;

public sealed record ActivateCourseCommand(string CourseIdKey, string UserId) 
    : IRequest<Result<CourseStatusUpdateResponse>>;
