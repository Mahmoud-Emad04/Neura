using Microsoft.AspNetCore.Identity;
using GraduationProject.Core.Abstractions;
using GraduationProject.Core.Authentication;
using GraduationProject.Core.Contracts.Authentication;
using System.Security.Cryptography;
using System.Text;
using GraduationProject.Core.Entities;
using GraduationProject.Core.Errors;
using GraduationProject.Core.Service;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace GraduationProject.Services.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    IJwtProvider jwtProvider,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    //private readonly IEmailSender _emailSender = emailSender;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly int _refreshTokenExpiryDays = 14;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AuthResponse>> GetTokenAsync(string userNameOrEmail, string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == userNameOrEmail || u.UserName == userNameOrEmail , cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        if (!isValidPassword)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var (token, expires) = _jwtProvider.GenerateToken(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = refreshToken,
            ExpiresOn = refreshTokenExpiry
        });

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(user.Id,user.UserName!, user.Email, user.FirstName, user.LastName, token, expires,
            refreshToken,
            refreshTokenExpiry);

        return Result.Success(response);
    }

    public async Task<Result<AuthResponse>> GetRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var userId = _jwtProvider.ValidateToken(token);

        if (userId is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var userRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken && rt.IsActive);

        if (userRefreshToken is null)
            return Result.Failure<AuthResponse>(UserErrors.InValidRefreshToken);

        var (newtoken, expires) = _jwtProvider.GenerateToken(user);
        var newrefreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        userRefreshToken.Revoked = DateTime.UtcNow;

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = newrefreshToken,
            ExpiresOn = refreshTokenExpiry
        });

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(user.Id,user.UserName!, user.Email, user.FirstName, user.LastName, newtoken, expires,
            newrefreshToken,
            refreshTokenExpiry);

        return Result.Success(response);
    }

    public async Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var userId = _jwtProvider.ValidateToken(token);

        if (userId is null)
            return Result.Failure(UserErrors.UserNotFound);

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
            return Result.Failure(UserErrors.UserNotFound);

        var userRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshToken && rt.IsActive);

        if (userRefreshToken is null)
            return Result.Failure(UserErrors.InValidRefreshToken);

        userRefreshToken.Revoked = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        return Result.Success();
    }

    public async Task<Result> RegisterAsync(RegisterRequest registerRequest,
        CancellationToken cancellationToken = default)
    {
        var isEmailExist = await _userManager.Users.AnyAsync(u => u.Email == registerRequest.Email, cancellationToken);

        if (isEmailExist)
            return Result.Failure(UserErrors.DuplicatedEmail);

        var username = registerRequest.UserName.Trim().ToUpperInvariant();

        var isUserNameExist =
            await _userManager.Users.AnyAsync(u => u.NormalizedUserName == username, cancellationToken);

        if (isUserNameExist)
            return Result.Failure(UserErrors.DuplicatedUserName);

        var user = registerRequest.Adapt<ApplicationUser>();
        var result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (result.Succeeded)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            _logger.LogInformation("User with {UserName} & {Code} Has been created.", user.UserName, code);

            //await SendConfirmationEmail(user, code);
            //BackgroundJob.Enqueue(() => SendConfirmationEmail(user, code));

            return Result.Success();
        }

        var error = result.Errors.FirstOrDefault();

        return Result.Failure(new Error(error!.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        if (await _userManager.FindByIdAsync(request.UserId) is not { } user)
            return Result.Failure(UserErrors.UserNotFound);

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);

        string code;

        try
        {
            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
        }
        catch (FormatException)
        {
            return Result.Failure(UserErrors.InvalidCode);
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}