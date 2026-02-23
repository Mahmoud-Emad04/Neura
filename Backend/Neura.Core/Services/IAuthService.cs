using Microsoft.AspNetCore.Authentication;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Authentication;

namespace Neura.Core.Services;

public interface IAuthService
{
    public Task<Result<AuthResponse>> GetTokenAsync(string email, string password,
        CancellationToken cancellationToken = default);

    public Task<Result<AuthResponse>> GetRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default);

    public Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> RegisterAsync(RegisterRequest registerRequest,
        CancellationToken cancellationToken = default);

    Task<Result<AuthResponse>> ConfirmEmailAsync(ConfirmEmailRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request);
    Task<Result> SendResetPasswordCodeAsync(string email);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
    AuthenticationProperties GetExternalAuthProperties(string provider, string redirectUrl);
    Task<ExternalAuthResult> HandleExternalLoginAsync();
}