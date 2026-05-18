using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Section;

namespace Neura.Api.Features.Sections.GetSectionById;

public sealed record GetSectionByIdQuery(int SectionId) 
    : IRequest<Result<SectionResponse>>;
