using MediatR;
using Neura.Core.Abstractions;

namespace Neura.Api.Features.Announcements.RemovePost;

public sealed record RemovePostCommand(int PostId, string UserId) 
    : IRequest<Result>;
