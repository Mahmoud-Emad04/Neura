using Neura.Api.Validation.File.commen;
using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Validation.Lesson;

public class CompleteLessonRequestValidator : AbstractValidator<CompleteLessonRequest>
{
    public CompleteLessonRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ScheduledDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Scheduled date must be in the future.")
            .When(x => x.ScheduledDate is not null);

        RuleFor(x => x.VideoFile)
            .SetValidator(new FileSizeValidator()!)
            .SetValidator(new BlockedSignaturesValidator()!)
            .SetValidator(new FileNameValidator()!)
            .When(x => x.VideoFile is not null);
    }
}
