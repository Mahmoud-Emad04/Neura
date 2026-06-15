using MediatR;
using Microsoft.AspNetCore.Identity;
using Neura.Core.Authentication;
using Neura.Core.Errors;

namespace Neura.Api.Features.Auth.RevokeToken;

internal sealed class RevokeTokenHandler(
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider)
    : IRequestHandler<RevokeTokenCommand, Result>
{
    public async Task<Result> Handle(
        RevokeTokenCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = jwtProvider.ValidateToken(request.Token);

        if (userId is null)
            return Result.Failure(UserErrors.UserNotFound);

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var userRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken && rt.IsActive);

        if (userRefreshToken is null)
            return Result.Failure(UserErrors.InValidRefreshToken);

        userRefreshToken.Revoked = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        return Result.Success();
    }
}
