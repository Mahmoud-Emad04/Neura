using MediatR;
using Neura.Core.Contracts.Section;

namespace Neura.Api.Features.Sections.UpdateSection;

public sealed record UpdateSectionCommand(int SectionId, SectionUpdateRequest Request, string UserId)
    : IRequest<Result<SectionResponse>>;
