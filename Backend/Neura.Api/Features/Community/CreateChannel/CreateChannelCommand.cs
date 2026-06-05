using MediatR;
using Neura.Core.Contracts.Community;

namespace Neura.Api.Features.Community.CreateChannel;

public sealed record CreateChannelCommand(int CourseId, CreateChannelRequest Request, string UserId) 
    : IRequest<ChannelDto>;
