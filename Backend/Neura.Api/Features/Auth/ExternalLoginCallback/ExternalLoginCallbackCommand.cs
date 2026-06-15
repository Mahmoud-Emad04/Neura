using MediatR;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ExternalLoginCallback;

public sealed record ExternalLoginCallbackCommand()
    : IRequest<Result<AuthResponse>>;
