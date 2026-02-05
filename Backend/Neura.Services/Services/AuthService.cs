using Hangfire;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authentication;
using Neura.Core.Contracts.Authentication;
using Neura.Services.Helpers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
namespace Neura.Services.Services;

public class AuthService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtProvider jwtProvider,
    IEmailSender emailSender,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly ApplicationDbContext _context = context;

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IJwtProvider _jwtProvider = jwtProvider;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly ILogger<AuthService> _logger = logger;
    private readonly int _refreshTokenExpiryDays = 14;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<AuthResponse>> GetTokenAsync(string userNameOrEmail, string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.SingleOrDefaultAsync(
            u => u.Email == userNameOrEmail || u.UserName == userNameOrEmail, cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        var passwordhash = new PasswordHasher<ApplicationUser>();
        _logger.LogInformation($"{passwordhash.HashPassword(user, password)}");

        if (!isValidPassword)
            return Result.Failure<AuthResponse>(UserErrors.UserNotFound);

        if (!user.EmailConfirmed)
            return Result.Failure<AuthResponse>(UserErrors.EmailNotConfirmed);

        var (userRoles, userPermissions) = await GetUserRolesAndPermissionsAsync(user, cancellationToken);
        var (token, expires) = _jwtProvider.GenerateToken(user, userRoles, userPermissions);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = refreshToken,
            ExpiresOn = refreshTokenExpiry
        });

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(user.Id, user.UserName!, user.DiscordHandle, user.Email!, user.FirstName,
            user.LastName, token, expires,
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

        var (userRoles, userPermissions) = await GetUserRolesAndPermissionsAsync(user, cancellationToken);
        var (newtoken, expires) = _jwtProvider.GenerateToken(user, userRoles, userPermissions);
        var newrefreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        userRefreshToken.Revoked = DateTime.UtcNow;

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = newrefreshToken,
            ExpiresOn = refreshTokenExpiry
        });


        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(user.Id, user.UserName!, user.DiscordHandle, user.Email!, user.FirstName,
            user.LastName, newtoken, expires,
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

            _logger.LogInformation("User with {UserName} & code {Code} Has been created.", user.UserName, code);

            //await SendConfirmationEmail(user, code);
            BackgroundJob.Enqueue(() => SendConfirmationEmail(user, code));

            return Result.Success();
        }

        var error = result.Errors.FirstOrDefault();

        return Result.Failure(new Error(error!.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task<Result<AuthResponse>> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        if (await _userManager.FindByIdAsync(request.UserId) is not { } user)
            return Result.Failure<AuthResponse>(UserErrors.InvalidCodeOrUser);

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

        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, DefaultRoles.Member);
            var (userRoles, userPermissions) = await GetUserRolesAndPermissionsAsync(user, cancellationToken);
            var (token, expires) = _jwtProvider.GenerateToken(user, userRoles, userPermissions);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

            user.RefreshTokens.Add(new RefreshTokens
            {
                Token = refreshToken,
                ExpiresOn = refreshTokenExpiry
            });

            await _userManager.UpdateAsync(user);

            var response = new AuthResponse(user.Id, user.UserName!, user.DiscordHandle, user.Email!, user.FirstName,
                user.LastName, token, expires,
                refreshToken,
                refreshTokenExpiry);

            return Result.Success(response);
        }

        var error = result.Errors.First();

        return Result.Failure<AuthResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }


    public async Task<Result> ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not { } user)
            return Result.Success();

        if (user.EmailConfirmed)
            return Result.Failure(UserErrors.DuplicatedConfirmation);

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        _logger.LogInformation("Confirmation code: {code}", code);

        // await SendConfirmationEmail(user, code);
        BackgroundJob.Enqueue(() => SendConfirmationEmail(user, code));

        return Result.Success();
    }

    public async Task<Result> SendResetPasswordCodeAsync(string email)
    {
        if (await _userManager.FindByEmailAsync(email) is not { } user)
            return Result.Success();

        if (!user.EmailConfirmed)
            return Result.Failure(UserErrors.EmailNotConfirmed);

        //{
        //    var otp = _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        //    _logger.LogWarning("OTP is {otp}", otp.Result);
        //}


        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        _logger.LogInformation("Reset code: {code}", code);

        BackgroundJob.Enqueue(() => SendResetPasswordEmail(user, code));

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.EmailConfirmed)
            return Result.Failure(UserErrors.InvalidCode);

        IdentityResult result;

        try
        {
            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Code));
            result = await _userManager.ResetPasswordAsync(user, code, request.NewPassword);
        }
        catch (FormatException)
        {
            result = IdentityResult.Failed(_userManager.ErrorDescriber.InvalidToken());
        }

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status401Unauthorized));
    }

    public AuthenticationProperties GetExternalAuthProperties(string provider, string redirectUrl)
    {
        return _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
    }

    public async Task<ExternalAuthResult> HandleExternalLoginAsync()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return new ExternalAuthResult(false, null, null, "ExternalAuthFailed");

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            false,
            true);

        ApplicationUser? user;

        if (result.Succeeded)
        {
            // Scenario A: User Exists and is Linked
            user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        }
        else
        {
            // Scenario B: New User or Link Existing Email
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(email))
                return new ExternalAuthResult(false, null, null, "EmailNotFound");

            user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = name?.Split(" ").FirstOrDefault() ?? "User",
                    LastName = name?.Split(" ").LastOrDefault() ?? "",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return new ExternalAuthResult(false, null, null, errors);
                }
            }

            var linkResult = await _userManager.AddLoginAsync(user, info);
            if (!linkResult.Succeeded)
                return new ExternalAuthResult(false, null, null, "LinkFailed");
        }

        if (user == null) return new ExternalAuthResult(false, null, null, "UserCreationFailed");

        // 5. Generate JWT (Using the Helper)
        // Pass CancellationToken.None since this method doesn't take one
        var authResult = await GenerateAuthResponseAsync(user, CancellationToken.None);

        if (authResult.IsFailure) return new ExternalAuthResult(false, null, null, authResult.Error.Code);

        var response = authResult.Value;

        return new ExternalAuthResult(
            true,
            response.Token,
            response.RefreshToken,
            null
        );
    }

    public async Task SendConfirmationEmail(ApplicationUser user, string code)
    {
        var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin;

        var request = _httpContextAccessor.HttpContext?.Request;

        if (string.IsNullOrEmpty(origin))
            origin = "http://localhost:5173";
        //origin = $"{request.Scheme}://{request.Host}{request.PathBase}";

        var emailBody = EmailBodyBuilder.GenerateEmailBody("EmailConfirmation",
            templateModel: new Dictionary<string, string>
            {
                { "{{name}}", user.FirstName },
                { "{{action_url}}", $"{origin}/auth/verify-email?userId={user.Id}&code={code}" }
            }
        );
        BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(user.Email!, "✅ Neura: Email Confirmation", emailBody));
        await Task.CompletedTask;
    }

    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var (userRoles, userPermissions) = await GetUserRolesAndPermissionsAsync(user, cancellationToken);

        var (token, expires) = _jwtProvider.GenerateToken(user, userRoles, userPermissions);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        user.RefreshTokens.Add(new RefreshTokens
        {
            Token = refreshToken,
            ExpiresOn = refreshTokenExpiry,
            CreatedOn = DateTime.UtcNow
        });

        await _userManager.UpdateAsync(user);

        var response = new AuthResponse(
            user.Id,
            user.UserName!,
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

    public async Task SendResetPasswordEmail(ApplicationUser user, string code)
    {
        var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin;

        var request = _httpContextAccessor.HttpContext?.Request;

        if (string.IsNullOrEmpty(origin))
            origin = "http://localhost:5173";
        //origin = $"{request.Scheme}://{request.Host}{request.PathBase}";

        var emailBody = EmailBodyBuilder.GenerateEmailBody("ForgetPassword",
            templateModel: new Dictionary<string, string>
            {
                { "{{name}}", user.FirstName },
                { "{{action_url}}", $"{origin}/auth/reset-password?email={user.Email}&code={code}" }
            }
        );

        BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(user.Email!, "✅ Survey Basket: Change Password", emailBody));

        await Task.CompletedTask;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private async Task<(IEnumerable<string> roles, IEnumerable<string> permissions)> GetUserRolesAndPermissionsAsync(
        ApplicationUser user, CancellationToken cancellationToken)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var userPermissions = await (
                from r in _context.Roles
                join c in _context.RoleClaims
                    on r.Id equals c.RoleId
                where userRoles.Contains(r.Name!)
                select c.ClaimValue
            ).Distinct()
            .ToListAsync(cancellationToken);

        return (userRoles, userPermissions);
    }
}