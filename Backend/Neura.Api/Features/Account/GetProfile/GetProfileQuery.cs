using MediatR;
using Neura.Core.Contracts.Users;

namespace Neura.Api.Features.Account.GetProfile;

public sealed record GetProfileQuery(string UserId)
    : IRequest<Result<UserProfileResponse>>;
