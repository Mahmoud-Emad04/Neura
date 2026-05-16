using Neura.Core.Contracts.ExamAttempt;

namespace Neura.Api.Validation.ExamAttempt;

public class SaveAnswerValidator : AbstractValidator<SaveAnswerRequest>
{
    public SaveAnswerValidator()
    {
        RuleFor(x => x.SelectedOptionIds)
            .NotNull()
            .WithMessage("SelectedOptionIds cannot be null.");
    }
}