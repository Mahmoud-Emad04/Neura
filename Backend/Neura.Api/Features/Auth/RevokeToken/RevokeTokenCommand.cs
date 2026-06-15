using MediatR;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.RevokeToken;

public sealed record RevokeTokenCommand(RefreshTokenRequest Request)
    : IRequest<Result>;
