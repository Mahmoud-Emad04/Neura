using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Validation.Auth;

public class AuthRequestValidator : AbstractValidator<LoginRequest>
{
    public AuthRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty().WithMessage("Username or email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}