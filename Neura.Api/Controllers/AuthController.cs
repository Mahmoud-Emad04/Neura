using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("")]
    public async Task<IActionResult> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging with email or username : {emailOrUsername} and password {password}", loginRequest.UserNameOrEmail,
            loginRequest.Password);

        var authResponse =
            await _authService.GetTokenAsync(loginRequest.UserNameOrEmail, loginRequest.Password, cancellationToken);

        return authResponse.IsSuccess
            ? Ok(authResponse.Value)
            : authResponse.ToProblem();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var authResponse =
            await _authService.GetRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return authResponse.IsSuccess
            ? Ok(authResponse.Value)
            : authResponse.ToProblem();
    }

    [HttpPost("revoke-refresh-token")]
    public async Task<IActionResult> RevokeRefreshTokenAsync([FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var isRevoked =
            await _authService.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);

        return isRevoked.IsSuccess
            ? Ok()
            : isRevoked.ToProblem();
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(RegisterRequest registerRequest, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(registerRequest, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmailAsync(ConfirmEmailRequest confirmEmailRequest)
    {
        var result = await _authService.ConfirmEmailAsync(confirmEmailRequest);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }
    [HttpPost("resend-confirm-email")]
    public async Task<IActionResult> ResendConfirmationEmailAsync([FromBody] ResendConfirmationEmailRequest request)
    {
        var result = await _authService.ResendConfirmationEmailAsync(request);

        return result.IsSuccess
            ? Ok()
            : result.ToProblem();
    }

    [HttpPost("forget-password")]
    public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request)
    {
        var result = await _authService.SendResetPasswordCodeAsync(request.Email);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);

        return result.IsSuccess ? Ok() : result.ToProblem();
    }
}