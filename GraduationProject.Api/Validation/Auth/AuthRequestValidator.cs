using GraduationProject.Core.Contracts.Authentication;

namespace GraduationProject.Api.Validation.Auth;

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