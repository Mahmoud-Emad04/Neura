using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using System.Text;

namespace Neura.Api.Features.Auth.ConfirmEmail;

internal sealed class ConfirmEmailHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    IServiceHelpers helpers)
    : IRequestHandler<ConfirmEmailCommand, Result<AuthResponse>>
{
    private const int RefreshTokenExpiryDays = 14;

    public async Task<Result<AuthResponse>> Handle(
        ConfirmEmailCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (await userManager.FindByIdAsync(request.UserId) is not { } user)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        if (user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.DuplicatedConfirmation);

        string code;

        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidCode);
        }

        var result = await userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, DefaultRoles.Member);
            return await AuthHelpers.GenerateAuthResponseAsync(user, context, userManager, jwtProvider, helpers, RefreshTokenExpiryDays, ct);
        }

        var error = result.Errors.First();

        return Result.Failure<AuthResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }
}
