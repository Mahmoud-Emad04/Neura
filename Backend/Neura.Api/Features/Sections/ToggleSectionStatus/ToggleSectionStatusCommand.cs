using MediatR;

namespace Neura.Api.Features.Sections.ToggleSectionStatus;

public sealed record ToggleSectionStatusCommand(int SectionId, string UserId)
    : IRequest<Result>;
