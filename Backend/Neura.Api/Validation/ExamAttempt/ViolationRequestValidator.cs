using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Validation.ExamAttempt;

public class ViolationRequestValidator : AbstractValidator<ViolationRequest>
{
    public ViolationRequestValidator()
    {
        RuleFor(x => x.ViolationType).IsInEnum();
        RuleFor(x => x.OccurredAt).NotEmpty();
    }
}