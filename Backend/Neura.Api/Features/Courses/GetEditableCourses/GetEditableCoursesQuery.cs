using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;

namespace Neura.Api.Features.Courses.GetEditableCourses;

public sealed record GetEditableCoursesQuery(EditableCourseFilters Filters, string UserId) 
    : IRequest<Result<EditableCoursesListSummaryResponse>>;
