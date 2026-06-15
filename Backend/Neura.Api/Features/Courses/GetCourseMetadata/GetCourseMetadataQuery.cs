using MediatR;

namespace Neura.Api.Features.Courses.GetCourseMetadata;

public sealed record GetCourseMetadataQuery(string CourseIdKey, string? UserId)
    : IRequest<Result<CourseMetadataResponse>>;
