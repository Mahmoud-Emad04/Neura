using GraduationProject.Core.Contracts.Authentication;

namespace GraduationProject.Api.Validation.Auth;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(r => r.Token)
            .NotEmpty();
        RuleFor(r => r.RefreshToken)
            .NotEmpty();
    }
}