using MediatR;

namespace Neura.Api.Features.Tags.DeleteTag;

public sealed record DeleteTagCommand(int Id, bool Force, string UserId)
    : IRequest<Result>;
