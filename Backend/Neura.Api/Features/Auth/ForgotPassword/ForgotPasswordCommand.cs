using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(ForgetPasswordRequest Request, string? Origin) 
    : IRequest<Result>;
