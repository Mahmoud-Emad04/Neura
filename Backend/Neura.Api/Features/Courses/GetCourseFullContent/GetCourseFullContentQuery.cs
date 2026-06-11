using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;

namespace Neura.Api.Features.Courses.GetCourseFullContent;

public sealed record GetCourseFullContentQuery(string CourseIdKey)
    : IRequest<Result<List<CourseFullContentResponse>>>;
