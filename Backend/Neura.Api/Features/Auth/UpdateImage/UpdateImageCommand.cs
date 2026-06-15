using MediatR;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Features.Auth.UpdateImage;

public sealed record UpdateImageCommand(UploadImageRequest Request, string UserId)
    : IRequest<Result<string>>;
