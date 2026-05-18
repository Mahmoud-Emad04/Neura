using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.CreateCourse;

public sealed record CreateCourseCommand(CourseRequest Request, string UserId) 
    : IRequest<Result<CourseMetadataResponse>>;
