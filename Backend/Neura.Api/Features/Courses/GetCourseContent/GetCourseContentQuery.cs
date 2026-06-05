using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.GetCourseContent;

public sealed record GetCourseContentQuery(string CourseIdKey, string? UserId) 
    : IRequest<Result<CourseResponse>>;
