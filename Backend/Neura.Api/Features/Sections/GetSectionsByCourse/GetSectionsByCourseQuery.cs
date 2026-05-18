using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;

namespace Neura.Api.Features.Sections.GetSectionsByCourse;

public sealed record GetSectionsByCourseQuery(string CourseIdKey) 
    : IRequest<Result<IEnumerable<SectionResponse>>>;
