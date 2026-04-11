using Neura.Core.Contracts.common;

namespace Neura.Api.Validation.Common;

public class RequestFiltersValidator : AbstractValidator<RequestFilters>
{
    private static readonly string[] AllowedSortDirections = ["ASC", "DESC"];

    public RequestFiltersValidator()
    {
        RuleFor(f => f.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(f => f.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Page size must be between 1 and 50.");

        RuleFor(f => f.SearchValue)
            .MaximumLength(200)
            .When(f => f.SearchValue is not null);

        RuleFor(f => f.SortDirection)
            .Must(d => AllowedSortDirections.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'ASC' or 'DESC'.")
            .When(f => f.SortDirection is not null);
    }
}