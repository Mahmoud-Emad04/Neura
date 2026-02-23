using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Validation.Lesson;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .Length(3, 250);

        RuleFor(x => x.SectionId)
            .GreaterThan(0)
            .WithMessage("Section ID must be a positive number.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Lesson type is not valid.");
    }
}
