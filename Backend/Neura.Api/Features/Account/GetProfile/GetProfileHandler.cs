using Mapster;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Users;
using Neura.Core.Entities;

namespace Neura.Api.Features.Account.GetProfile;

internal sealed class GetProfileHandler(UserManager<ApplicationUser> userManager) 
    : IRequestHandler<GetProfileQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(
        GetProfileQuery query, CancellationToken ct)
    {
        var user = await userManager.Users
            .Where(u => u.Id == query.UserId)
            .ProjectToType<UserProfileResponse>()
            .SingleAsync(ct);

        return Result.Success(user);
    }
}
