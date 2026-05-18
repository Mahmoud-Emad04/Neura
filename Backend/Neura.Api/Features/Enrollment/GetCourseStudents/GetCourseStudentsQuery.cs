using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Enrollment;

namespace Neura.Api.Features.Enrollment.GetCourseStudents;

public sealed record GetCourseStudentsQuery(int CourseId, string RequesterId, int PageNumber, int PageSize) 
    : IRequest<Result<CourseStudentsListResponse>>;
