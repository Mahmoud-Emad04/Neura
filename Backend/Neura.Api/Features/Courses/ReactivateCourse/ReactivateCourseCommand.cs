using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.ReactivateCourse;

public sealed record ReactivateCourseCommand(string CourseIdKey, string UserId) 
    : IRequest<Result<CourseStatusUpdateResponse>>;
