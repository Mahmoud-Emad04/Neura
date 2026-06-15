using MediatR;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.GetCourseStatus;

public sealed record GetCourseStatusQuery(string CourseIdKey, string UserId)
    : IRequest<Result<CourseStatusResponse>>;
