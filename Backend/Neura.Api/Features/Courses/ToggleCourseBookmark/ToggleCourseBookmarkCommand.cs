using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Courses.ToggleCourseBookmark;

public sealed record ToggleCourseBookmarkCommand(string CourseIdKey, string UserId) 
    : IRequest<Result>;
