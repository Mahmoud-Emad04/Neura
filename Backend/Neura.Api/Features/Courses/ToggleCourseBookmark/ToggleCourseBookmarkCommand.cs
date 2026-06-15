using MediatR;

namespace Neura.Api.Features.Courses.ToggleCourseBookmark;

public sealed record ToggleCourseBookmarkCommand(string CourseIdKey, string UserId)
    : IRequest<Result>;
