using Neura.Core.Contracts.Question;
using Neura.Core.Enums;

namespace Neura.Api.Validation.Question;

public class UpdateQuestionValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.QuestionType).IsInEnum();
        RuleFor(x => x.Points).GreaterThan(0);

        RuleFor(x => x.Options)
            .NotEmpty()
            .WithMessage("A question must have at least one option.");

        When(x => x.QuestionType == QuestionType.TrueFalse, () =>
        {
            RuleFor(x => x.Options).Must(o => o.Count == 2)
                .WithMessage("True/False questions must have exactly 2 options.");
            RuleFor(x => x.Options).Must(o => o.Count(opt => opt.IsCorrect) == 1)
                .WithMessage("True/False questions must have exactly 1 correct answer.");
        });

        When(x => x.QuestionType == QuestionType.SingleChoice, () =>
        {
            RuleFor(x => x.Options).Must(o => o.Count >= 2)
                .WithMessage("Single choice questions must have at least 2 options.");
            RuleFor(x => x.Options).Must(o => o.Count(opt => opt.IsCorrect) == 1)
                .WithMessage("Single choice questions must have exactly 1 correct answer.");
        });

        When(x => x.QuestionType == QuestionType.MultipleChoice, () =>
        {
            RuleFor(x => x.Options).Must(o => o.Count >= 2)
                .WithMessage("Multiple choice questions must have at least 2 options.");
            RuleFor(x => x.Options).Must(o => o.Count(opt => opt.IsCorrect) >= 1)
                .WithMessage("Multiple choice questions must have at least 1 correct answer.");
            RuleFor(x => x.Options).Must(o => o.Count(opt => !opt.IsCorrect) >= 1)
                .WithMessage("Multiple choice questions must have at least 1 incorrect answer.");
        });

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Text).NotEmpty().MaximumLength(1000);
        });
    }
}