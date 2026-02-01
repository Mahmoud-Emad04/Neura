namespace Neura.Api.Validation.Course;

public class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().Length(3, 100);

        RuleFor(c => c.Description).NotEmpty().Length(3, 1000);

        RuleFor(c => c.Startin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(c => c.Endin)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(c => c.Startin);

        RuleFor(c => c.Tags).NotEmpty();

        RuleFor(c => c.Tags)
            .Must((request, context) => request.Tags.Distinct().Count() == request.Tags.Count())
            .WithMessage("Tags must be unique")
            .When(c => c.Tags is not null);
    }
}