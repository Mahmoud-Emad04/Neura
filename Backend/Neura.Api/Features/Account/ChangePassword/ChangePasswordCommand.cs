using MediatR;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Features.Account.ChangePassword;

public sealed record ChangePasswordCommand(string UserId, ChangePasswordRequest Request) 
    : IRequest<Result>;
