using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;

namespace Neura.Api.Features.Courses.GetCourseFullContent;

public sealed record GetCourseFullContentQuery()
    : IRequest<Result<List<CourseFullContentResponse>>>;
