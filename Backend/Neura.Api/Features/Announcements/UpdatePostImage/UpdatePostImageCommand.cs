using MediatR;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Features.Announcements.UpdatePostImage;

public sealed record UpdatePostImageCommand(int PostId, UploadImageRequest Request, string UserId)
    : IRequest<Result>;
