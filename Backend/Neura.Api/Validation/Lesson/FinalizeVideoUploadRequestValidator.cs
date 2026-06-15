using Neura.Core.Contracts.Lessons;

namespace Neura.Api.Validation.Lesson;

public class FinalizeVideoUploadRequestValidator : AbstractValidator<FinalizeVideoUploadRequest>
{
    public FinalizeVideoUploadRequestValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty()
            .WithMessage("Public ID is required.")
            .MaximumLength(255)
            .WithMessage("Public ID cannot exceed 255 characters.");

        RuleFor(x => x.VideoUrl)
            .NotEmpty()
            .WithMessage("Video URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Video URL must be a valid URI.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0)
            .WithMessage("Duration must be greater than 0 seconds.")
            .LessThanOrEqualTo(86400) // 24 hours max
            .WithMessage("Duration cannot exceed 24 hours (86400 seconds).");
    }
}
