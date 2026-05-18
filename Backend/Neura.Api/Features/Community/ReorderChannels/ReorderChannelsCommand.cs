using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.ReorderChannels;

public sealed record ReorderChannelsCommand(int CourseId, ReorderChannelsRequest Request, string UserId) 
    : IRequest<IReadOnlyList<ChannelDto>>;
