using MediatR;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.UpdatePost;

public sealed record UpdatePostCommand(int PostId, PostUpdateRequest Request, string UserId)
    : IRequest<Result<PostResponse>>;
