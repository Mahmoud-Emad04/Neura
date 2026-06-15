using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.GetVoiceParticipants;

internal sealed class GetVoiceParticipantsHandler(IVoiceChannelService voiceService)
    : IRequestHandler<GetVoiceParticipantsQuery, IReadOnlyList<VoiceParticipantDto>>
{
    public async Task<IReadOnlyList<VoiceParticipantDto>> Handle(
        GetVoiceParticipantsQuery query, CancellationToken ct)
    {
        return await voiceService.GetParticipantsAsync(query.ChannelId, query.UserId);
    }
}
