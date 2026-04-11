using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Validation.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Code)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(RegexPatterns.Password)
            .WithMessage(
                "Password must be at least 8 characters and include at least one lowercase letter, one uppercase letter, one digit, and one special character.");
    }
}