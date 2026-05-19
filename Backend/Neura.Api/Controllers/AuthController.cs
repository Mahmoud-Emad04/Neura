using MediatR;
using Neura.Api.Extensions;
using Neura.Api.Features.Auth.ConfirmEmail;
using Neura.Api.Features.Auth.ExternalLoginCallback;
using Neura.Api.Features.Auth.ForgotPassword;
using Neura.Api.Features.Auth.Login;
using Neura.Api.Features.Auth.Refresh;
using Neura.Api.Features.Auth.Register;
using Neura.Api.Features.Auth.ResendConfirmation;
using Neura.Api.Features.Auth.ResetPassword;
using Neura.Api.Features.Auth.RevokeToken;
using Neura.Api.Features.Auth.UpdateImage;
using Neura.Core.Contracts.Authentication;
using Neura.Core.Contracts.Files;
using Neura.Core.Contracts.Users;
using Microsoft.AspNetCore.Authentication;

namespace Neura.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(ISender sender, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    ///     Logs in a user and returns a JWT token.
    ///     Route: POST /auth/login
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest loginRequest, CancellationToken ct)
    {
        var command = new LoginCommand(loginRequest);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Registers a new user.
    ///     Route: POST /auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest registerRequest, CancellationToken ct)
    {
        var command = new RegisterCommand(registerRequest, Request.Headers.Origin.FirstOrDefault());
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPut("image")]
    [Authorize]
    public async Task<IActionResult> UpdateImage([FromForm] UploadImageRequest request, CancellationToken ct)
    {
        var command = new UpdateImageCommand(request, User.GetUserId()!);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Refreshes an expired JWT token.
    ///     Route: POST /auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RefreshCommand(request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Revokes a refresh token (Logout).
    ///     Route: POST /auth/revoke
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var command = new RevokeTokenCommand(request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Confirms the user's email address using a code.
    ///     Route: POST /auth/confirm-email
    /// </summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var command = new ConfirmEmailCommand(request);
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    /// <summary>
    ///     Resends the email confirmation code.
    ///     Route: POST /auth/resend-confirmation
    /// </summary>
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationEmailRequest request, CancellationToken ct)
    {
        var command = new ResendConfirmationCommand(request, Request.Headers.Origin.FirstOrDefault());
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Sends a password reset link/code to the user's email.
    ///     Route: POST /auth/forgot-password
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(request, Request.Headers.Origin.FirstOrDefault());
        var result = await sender.Send(command, ct);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    /// <summary>
    ///     Resets the password using the token received in email.
    ///     Route: POST /auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(request);
        var result = await sender.Send(command, ct);

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

        // Keep the AuthenticationProperties manually as it's purely framework level.
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["LoginProvider"] = provider;
        
        return Challenge(properties, provider);
    }

    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalLoginCallback(CancellationToken ct)
    {
        var command = new ExternalLoginCallbackCommand();
        var result = await sender.Send(command, ct);
        
        var frontendUrl = configuration["FrontendUrl"];

        if (!result.IsSuccess)
        {
            var safeError = Uri.EscapeDataString(result.Error?.Message ?? "unknown");
            return Redirect($"{frontendUrl}/login?error={safeError}");
        }

        return Redirect($"{frontendUrl}/callback#token={result.Value.Token}&refreshToken={result.Value.RefreshToken}");
    }
}