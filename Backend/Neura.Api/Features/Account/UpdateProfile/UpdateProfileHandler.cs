using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Neura.Api.Features.Account.UpdateProfile;

internal sealed class UpdateProfileHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateProfileCommand, Result>
{
    public async Task<Result> Handle(
        UpdateProfileCommand command, CancellationToken ct)
    {
        await userManager.Users
            .Where(x => x.Id == command.UserId)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(x => x.FirstName, command.Request.FirstName)
                    .SetProperty(x => x.LastName, command.Request.LastName),
                ct);

        return Result.Success();
    }
}
