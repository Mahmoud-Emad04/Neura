using MediatR;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Features.Account.UpdateProfile;

public sealed record UpdateProfileCommand(string UserId, UpdateProfileRequest Request)
    : IRequest<Result>;
