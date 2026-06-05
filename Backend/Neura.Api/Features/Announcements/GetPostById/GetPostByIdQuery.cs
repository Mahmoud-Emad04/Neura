using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Announcement;

namespace Neura.Api.Features.Announcements.GetPostById;

public sealed record GetPostByIdQuery(int PostId, string? CurrentUserId = null) 
    : IRequest<Result<PostResponse>>;
