using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Neura.Core.Abstractions;
using Neura.Core.Entities;
using Neura.Core.Errors;
using System.Text;

namespace Neura.Api.Features.Auth.ResetPassword;

internal sealed class ResetPasswordHandler(
    UserManager<ApplicationUser> userManager) 
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ResetPasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.EmailConfirmed)
            return Result.Failure(UserErrors.InvalidCode);

        IdentityResult result;

        try
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
            result = await userManager.ResetPasswordAsync(user, code, request.NewPassword);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
        }

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status401Unauthorized));
    }
}
