using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Courses.DeleteCourse;

public sealed record DeleteCourseCommand(string CourseIdKey, string UserId) 
    : IRequest<Result>;
