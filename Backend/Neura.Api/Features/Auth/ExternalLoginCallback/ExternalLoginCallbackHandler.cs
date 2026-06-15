using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using System.Security.Claims;

namespace Neura.Api.Features.Auth.ExternalLoginCallback;

internal sealed class ExternalLoginCallbackHandler(
    ApplicationDbContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    IServiceHelpers helpers)
    : IRequestHandler<ExternalLoginCallbackCommand, Result<AuthResponse>>
{
    private const int RefreshTokenExpiryDays = 14;

    public async Task<Result<AuthResponse>> Handle(
        ExternalLoginCallbackCommand command, CancellationToken ct)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return Result.Failure<AuthResponse>(new Error("ExternalAuth.Failed", "External authentication failed.", StatusCodes.Status400BadRequest));

        var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (user == null)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure<AuthResponse>(new Error("ExternalAuth.EmailNotFound", "Email from external provider not found.", StatusCodes.Status400BadRequest));

            user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "";
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var familyName = info.Principal.FindFirstValue(ClaimTypes.Surname);
                var picture = info.Principal.FindFirstValue("picture")
                              ?? info.Principal.FindFirstValue("urn:github:avatar_url");

                var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = givenName ?? parts.ElementAtOrDefault(0) ?? "User",
                    LastName = familyName ?? parts.ElementAtOrDefault(1) ?? "",
                    ImageUrl = string.IsNullOrWhiteSpace(picture) ? AuthHelpers.DefaultUserImagePath() : picture,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Result.Failure<AuthResponse>(new Error("ExternalAuth.CreationFailed", errors, StatusCodes.Status400BadRequest));
                }
            }

            var linkResult = await userManager.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
                return Result.Failure<AuthResponse>(new Error("ExternalAuth.LinkFailed", "Failed to link external login.", StatusCodes.Status400BadRequest));
        }

        await signInManager.SignOutAsync();

        return await AuthHelpers.GenerateAuthResponseAsync(user, context, userManager, jwtProvider, helpers, RefreshTokenExpiryDays, ct);
    }
}
