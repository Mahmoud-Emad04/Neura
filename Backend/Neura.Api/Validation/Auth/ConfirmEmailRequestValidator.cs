using Neura.Core.Contracts.Authentication;

namespace Neura.Api.Validation.Auth;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Code).NotEmpty();
    }
}