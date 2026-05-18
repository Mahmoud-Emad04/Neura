using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.GetCourseMetadata;

public sealed record GetCourseMetadataQuery(string CourseIdKey, string? UserId) 
    : IRequest<Result<CourseMetadataResponse>>;
