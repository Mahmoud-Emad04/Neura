using MediatR;

namespace Neura.Api.Features.Courses.GetCourseFullContent;

public sealed record GetCourseFullContentQuery()
    : IRequest<Result<List<CourseFullContentResponse>>>;
