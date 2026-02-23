using Neura.Core.Contracts.Review;

namespace Neura.Api.Validation.Review;

public class ReviewRequestValidator : AbstractValidator<ReviewRequest>
{
    public ReviewRequestValidator()
    {
        RuleFor(c => c.Rating)
            .NotNull()
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(5)
            .WithMessage("The rating must be between 1 and 5 stars.");
    }
}