using MediatR;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.Register;

public sealed record RegisterCommand(RegisterRequest Request, string? Origin)
    : IRequest<Result>;
