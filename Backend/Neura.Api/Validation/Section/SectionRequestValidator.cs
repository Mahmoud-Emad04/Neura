using Neura.Core.Contracts.Section;

namespace Neura.Api.Validation.Section;

public class SectionRequestValidator : AbstractValidator<SectionRequest>
{
	public SectionRequestValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

		RuleFor(x => x.Description)
			.MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
			.When(x => x.Description != null);

		// Position basic validation only; uniqueness and sequential checks are performed in the service layer
		RuleFor(x => x.Position)
			.GreaterThanOrEqualTo(1).WithMessage("Position must be 1 or greater.")
			.LessThanOrEqualTo(1000).WithMessage("Position must be 1000 or less.");
	}
}
