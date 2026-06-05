using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.CreatePost;

public sealed record CreatePostCommand(PostRequest Request, string UserId) 
    : IRequest<Result<PostResponse>>;
