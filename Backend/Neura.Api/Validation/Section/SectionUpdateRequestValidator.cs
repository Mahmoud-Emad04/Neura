using Neura.Core.Contracts.Section;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Validation.Section;

public class SectionUpdateRequestValidator : AbstractValidator<SectionUpdateRequest>
{
    public SectionUpdateRequestValidator(ApplicationDbContext db, IHttpContextAccessor http, IServiceHelpers helpers)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(250).WithMessage("Title must not exceed 250 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(1).WithMessage("Position must be 1 or greater.")
            .Must(x => x <= 1000).WithMessage("Position must be less than or equal to 1000");
    }
}