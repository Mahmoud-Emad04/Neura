using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neura.Core.Abstractions;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Auth.Login;

internal sealed class LoginHandler(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    IServiceHelpers helpers,
    ILogger<LoginHandler> logger) 
    : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private const int RefreshTokenExpiryDays = 14;

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand command, CancellationToken ct)
    {
        var userNameOrEmail = command.Request.UserNameOrEmail;
        var password = command.Request.Password;

        var user = await userManager.Users.SingleOrDefaultAsync(
            u => u.Email == userNameOrEmail || u.UserName == userNameOrEmail, ct);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        var isValidPassword = await userManager.CheckPasswordAsync(user, password);

        var passwordhash = new PasswordHasher<ApplicationUser>();
        logger.LogInformation($"{passwordhash.HashPassword(user, password)}");

        if (!isValidPassword)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

        if (!user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

        return await AuthHelpers.GenerateAuthResponseAsync(user, context, userManager, jwtProvider, helpers, RefreshTokenExpiryDays, ct);
    }
}
