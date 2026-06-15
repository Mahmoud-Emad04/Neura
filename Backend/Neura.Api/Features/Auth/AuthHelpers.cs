using Microsoft.AspNetCore.Identity;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;
using System.Security.Cryptography;

namespace Neura.Api.Features.Auth;

public static class AuthHelpers
{
    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string DefaultUserImagePath()
    {
        return Path.Combine("Images", ImageConsts.User, ImageConsts.DefaultUserImage);
    }

    public static async Task<(IEnumerable<string> roles, IEnumerable<string> permissions)> GetUserRolesAndPermissionsAsync(
        ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user, CancellationToken cancellationToken)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        var userPermissions = await (
                from r in context.Roles
                join c in context.RoleClaims
                    on r.Id equals c.RoleId
                where userRoles.Contains(r.Name!)
                select c.ClaimValue
            ).Distinct()
            .ToListAsync(cancellationToken);

        return (userRoles, userPermissions);
    }

    public static async Task<Result<AuthResponse>> GenerateAuthResponseAsync(
        ApplicationUser user,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IJwtProvider jwtProvider,
        IServiceHelpers helpers,
        int refreshTokenExpiryDays,
        CancellationToken cancellationToken)
    {
        var (userRoles, userPermissions) = await GetUserRolesAndPermissionsAsync(context, userManager, user, cancellationToken);

        var (token, expires) = jwtProvider.GenerateToken(user, userRoles, userPermissions);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = refreshToken,
            ExpiresOn = refreshTokenExpiry,
            CreatedOn = DateTime.UtcNow
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
            token,
            expires,
            refreshToken,
            refreshTokenExpiry
        );

        return Result.Success(response);
    }
}
