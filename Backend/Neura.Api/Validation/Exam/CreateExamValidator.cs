using Neura.Core.Contracts.Exam;

namespace Neura.Api.Validation.Exam;

public class CreateExamValidator : AbstractValidator<CreateExamRequest>
{
    public CreateExamValidator()
    {
        RuleFor(x => x.LessonId)
            .GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.DurationInMinutes)
            .GreaterThan(0)
            .When(x => x.DurationInMinutes.HasValue);

        RuleFor(x => x.PassingScorePercentage)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.MaxAttempts)
            .GreaterThan(0)
            .When(x => x.MaxAttempts.HasValue);

        RuleFor(x => x.NumberOfQuestionsToServe)
            .GreaterThan(0)
            .When(x => x.NumberOfQuestionsToServe.HasValue);

        RuleFor(x => x.MaxViolationsBeforeAutoSubmit)
            .GreaterThan(0)
            .When(x => x.MaxViolationsBeforeAutoSubmit.HasValue);

        RuleFor(x => x.MaxViolationsBeforeAutoSubmit)
            .NotNull()
            .When(x => x.EnableTabSwitchDetection)
            .WithMessage("MaxViolationsBeforeAutoSubmit is required when tab switch detection is enabled.");
    }
}