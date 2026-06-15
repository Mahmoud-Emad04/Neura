using MediatR;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(ResetPasswordRequest Request)
    : IRequest<Result>;
