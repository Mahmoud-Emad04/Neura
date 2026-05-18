using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.Login;

public sealed record LoginCommand(LoginRequest Request) 
    : IRequest<Result<AuthResponse>>;
