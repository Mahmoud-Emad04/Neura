using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Neura.Api.Features.Account.ChangePassword;

internal sealed class ChangePasswordHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(
        ChangePasswordCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null) return Result.Failure(new Error("UserNotFound", "User not found", StatusCodes.Status404NotFound));

        var result = await userManager.ChangePasswordAsync(user, command.Request.CurrentPassword, command.Request.NewPassword);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }
}
