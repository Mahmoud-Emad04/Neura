using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Validation.Lesson;

public class UpdateLessonPositionRequestValidator : AbstractValidator<UpdateLessonPositionRequest>
{
    public UpdateLessonPositionRequestValidator()
    {
        RuleFor(x => x.NewPosition)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Position must be at least 1");
    }
}

public class UpdateLessonPrivacyRequestValidator : AbstractValidator<UpdateLessonPrivacyRequest>
{
    public UpdateLessonPrivacyRequestValidator()
    {
        // No specific validation needed, boolean properties are always valid
    }
}

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title cannot exceed 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description cannot exceed 5000 characters");
    }
}
