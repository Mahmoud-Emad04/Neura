using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetVoiceParticipants;

public sealed record GetVoiceParticipantsQuery(int ChannelId, string UserId)
    : IRequest<IReadOnlyList<VoiceParticipantDto>>;
