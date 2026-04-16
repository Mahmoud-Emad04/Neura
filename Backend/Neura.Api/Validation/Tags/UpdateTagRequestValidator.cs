using Neura.Core.Contracts.Tags;

namespace Neura.Api.Validation.Tags;

public class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(100).WithMessage("Tag name cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9\s\-\.#\+]+$")
            .WithMessage("Tag name contains invalid characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.IconUrl)
            .MaximumLength(500).WithMessage("Icon URL cannot exceed 500 characters.")
            .Must(BeValidUrl).When(x => !string.IsNullOrWhiteSpace(x.IconUrl))
            .WithMessage("Icon URL must be a valid URL.");

        RuleFor(x => x.ColorHex)
            .Matches(@"^#?[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex))
            .WithMessage("Color must be a valid hex code (e.g., #FF5733).");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be zero or greater.");
    }

    private static bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}