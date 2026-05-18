using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.UpdateCourseDetails;

public sealed record UpdateCourseDetailsCommand(string CourseIdKey, CourseUpdateRequest Request, string UserId) 
    : IRequest<Result>;
