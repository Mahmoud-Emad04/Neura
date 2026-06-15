using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Validation.Lesson;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(3, 250);

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Lesson type is not valid.");

        // Position basic validation only; uniqueness and sequential checks are performed in the service layer
        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(1).WithMessage("Position must be 1 or greater.")
            .LessThanOrEqualTo(1000).WithMessage("Position must be 1000 or less.");
    }
}