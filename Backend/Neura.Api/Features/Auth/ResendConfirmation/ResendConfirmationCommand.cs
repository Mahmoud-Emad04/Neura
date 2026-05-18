using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ResendConfirmation;

public sealed record ResendConfirmationCommand(ResendConfirmationEmailRequest Request, string? Origin) 
    : IRequest<Result>;
