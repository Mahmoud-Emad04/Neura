using System.Text.RegularExpressions;

namespace Neura.Api.Features.Tags;

public static partial class TagHelpers
{
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Replace spaces with hyphens
        slug = slug.Replace(' ', '-');

        // Remove invalid characters (keep only letters, numbers, hyphens)
        slug = SlugRegex().Replace(slug, "");

        // Remove multiple consecutive hyphens
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    public static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        return slug.ToLowerInvariant().Trim();
    }

    public static bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        // Only lowercase letters, numbers, and hyphens
        return ValidSlugRegex().IsMatch(slug);
    }

    public static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return true; // Empty is valid (optional field)

        return HexColorRegex().IsMatch(color);
    }

    public static string? NormalizeHexColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return null;

        // Ensure uppercase and # prefix
        color = color.Trim().ToUpperInvariant();

        if (!color.StartsWith('#'))
            color = "#" + color;

        return color;
    }

    // Regex patterns (source generated for performance)
    [GeneratedRegex(@"[^a-z0-9\-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex ValidSlugRegex();

    [GeneratedRegex(@"^#?[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
