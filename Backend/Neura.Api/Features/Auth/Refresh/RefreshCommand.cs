using MediatR;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.Refresh;

public sealed record RefreshCommand(RefreshTokenRequest Request)
    : IRequest<Result<AuthResponse>>;
