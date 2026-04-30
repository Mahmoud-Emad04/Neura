using Neura.Api.Extensions;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Contracts.Files;

namespace Neura.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger,
    IConfiguration configuration) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<AuthController> _logger = logger;

    /// <summary>
    ///     Logs in a user and returns a JWT token.
    ///     Route: POST /auth/login
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest loginRequest, CancellationToken cancellationToken)
    {
        var authResponse =
            await _authService.GetTokenAsync(loginRequest.UserNameOrEmail, loginRequest.Password, cancellationToken);

        return authResponse.IsSuccess
            ? Ok(authResponse.Value)
            : authResponse.ToProblem();
    }

    /// <summary>
    ///     Registers a new user.
    ///     Route: POST /auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(registerRequest, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPut("image")]
    [Authorize]
    public async Task<IActionResult> UpdateImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.UpdateImageAsync(request, User.GetUserId()!, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }


    /// <summary>
    ///     Refreshes an expired JWT token.
    ///     Route: POST /auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.GetRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Revokes a refresh token (Logout).
    ///     Route: POST /auth/revoke
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Confirms the user's email address using a code.
    ///     Route: POST /auth/confirm-email
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.ConfirmEmailAsync(request, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Resends the email confirmation code.
    ///     Route: POST /auth/resend-confirmation
    /// </summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailRequest request)
    {
        var result = await _authService.ResendConfirmationEmailAsync(request);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Sends a password reset link/code to the user's email.
    ///     Route: POST /auth/forgot-password
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request)
    {
        var result = await _authService.SendResetPasswordCodeAsync(request.Email);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Resets the password using the token received in email.
    ///     Route: POST /auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }


    // ===============================
    // EXTERNAL AUTH (Google/GitHub)
    // ===============================

    [HttpGet("external-login/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var allowed = new[] { "Google", "GitHub" };
        if (!allowed.Contains(provider, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "unsupported_provider" });

        var redirectUrl = Url.Action(
            nameof(ExternalLoginCallback), "Auth",
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.Value);

        var properties = _authService.GetExternalAuthProperties(provider, redirectUrl!);
        return Challenge(properties, provider);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var result = await _authService.HandleExternalLoginAsync();
        var frontendUrl = _configuration["FrontendUrl"];

        if (!result.IsSuccess)
        {
            var safeError = Uri.EscapeDataString(result.ErrorMessage ?? "unknown");
            return Redirect($"{frontendUrl}/login?error={safeError}");
        }

        return Redirect(
            $"{frontendUrl}/callback#token={result.Token}&refreshToken={result.RefreshToken}");
    }
}