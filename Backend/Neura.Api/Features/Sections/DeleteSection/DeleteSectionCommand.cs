using MediatR;

namespace Neura.Api.Features.Sections.DeleteSection;

public sealed record DeleteSectionCommand(int SectionId, string UserId)
    : IRequest<Result>;
