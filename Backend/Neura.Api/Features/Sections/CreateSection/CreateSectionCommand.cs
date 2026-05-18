using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;

namespace Neura.Api.Features.Sections.CreateSection;

public sealed record CreateSectionCommand(string CourseIdKey, SectionRequest Request, string UserId) 
    : IRequest<Result<SectionResponse>>;
