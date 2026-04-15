using Neura.Api.Validation.File.commen;
using Neura.Core.Settings;

namespace Neura.Api.Validation.Course;

public class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().Length(3, 100);

        RuleFor(c => c.Description).NotEmpty().Length(3, 1000);

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be zero or a positive value.");

        RuleFor(c => c.Startin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(c => c.Endin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(c => c.Startin);

        RuleFor(c => c.Tags).NotEmpty();

        RuleFor(c => c.Tags)
            .Must(tags => tags.Distinct().Count() == tags.Count)
            .WithMessage("Tags must be unique.")
            .When(c => c.Tags is not null);

        RuleForEach(c => c.LearningOutcomes)
            .NotEmpty()
            .MaximumLength(500)
            .When(c => c.LearningOutcomes is not null);

        RuleForEach(c => c.Prerequisites)
            .NotEmpty()
            .MaximumLength(500)
            .When(c => c.Prerequisites is not null);

        RuleFor(r => r.Image)
            .SetValidator(new FileSizeValidator())
            .SetValidator(new BlockedSignaturesValidator())
            .SetValidator(new FileNameValidator())
            .Must((request, context) =>
            {
                var extension = Path.GetExtension(request.Image.FileName.ToLower());
                return FileSettings.AllowedImagesExtensions.Contains(extension);
            })
            .WithMessage("Invalid extension")
            .When(r => r.Image is not null);
    }
}