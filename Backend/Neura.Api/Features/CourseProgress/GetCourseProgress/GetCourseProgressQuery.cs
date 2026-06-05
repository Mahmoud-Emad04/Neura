using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Features.CourseProgress.GetCourseProgress;

public sealed record GetCourseProgressQuery(string CourseKeyId, string UserId) 
    : IRequest<Result<CourseProgressResponse>>;
