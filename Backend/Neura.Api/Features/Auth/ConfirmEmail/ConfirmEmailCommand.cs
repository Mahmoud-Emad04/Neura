using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(ConfirmEmailRequest Request) 
    : IRequest<Result<AuthResponse>>;
