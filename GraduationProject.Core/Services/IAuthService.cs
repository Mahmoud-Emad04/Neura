using GraduationProject.Core.Abstractions;
using GraduationProject.Core.Contracts.Authentication;

namespace GraduationProject.Core.Service;

public interface IAuthService
{
    public Task<Result<AuthResponse>> GetTokenAsync(string email, string password,
        CancellationToken cancellationToken = default!);

    public Task<Result<AuthResponse>> GetRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default!);

    public Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken,
        CancellationToken cancellationToken = default!);

    Task<Result> RegisterAsync(RegisterRequest registerRequest,
        CancellationToken cancellationToken = default);

 //   Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request);
  //  Task<Result> ResendConfirmationEmailAsync(ResendConfirmationEmailRequest request);
}