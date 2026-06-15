using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Auth.Refresh;

internal sealed class RefreshHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    IServiceHelpers helpers)
    : IRequestHandler<RefreshCommand, Result<AuthResponse>>
{
    private const int RefreshTokenExpiryDays = 14;

    public async Task<Result<AuthResponse>> Handle(
        RefreshCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = jwtProvider.ValidateToken(request.Token);

        if (userId is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var userRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken && rt.IsActive);

        if (userRefreshToken is null)
            return Result.Failure<AuthResponse>(UserErrors.InValidRefreshToken);

        var (userRoles, userPermissions) = await AuthHelpers.GetUserRolesAndPermissionsAsync(context, userManager, user, ct);
        var (newtoken, expires) = jwtProvider.GenerateToken(user, userRoles, userPermissions);
        var newrefreshToken = AuthHelpers.GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        userRefreshToken.Revoked = DateTime.UtcNow;

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = newrefreshToken,
            ExpiresOn = refreshTokenExpiry
        });

        await userManager.UpdateAsync(user);

        string baseUrl = helpers.GetBaseUrl();
        var response = new AuthResponse(
            user.Id,
            user.UserName!,
            $"{baseUrl}/{user.ImageUrl}",
            user.DiscordHandle,
            user.Email!,
            user.FirstName,
            user.LastName,
            newtoken,
            expires,
            newrefreshToken,
            refreshTokenExpiry);

        return Result.Success(response);
    }
}
