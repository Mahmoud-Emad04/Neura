using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class TagErrors
{
    // ══════════════════════════════════════════════════════════════
    // Not Found (404)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error TagNotFound =
        new("Tag.NotFound", "The specified tag was not found.", StatusCodes.Status404NotFound);

    public static readonly Error TagsNotFound =
        new("Tag.TagsNotFound", "One or more specified tags were not found.", StatusCodes.Status404NotFound);

    // ══════════════════════════════════════════════════════════════
    // Bad Request (400)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error InvalidName =
        new("Tag.InvalidName", "Tag name is required and cannot be empty.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidSlug =
        new("Tag.InvalidSlug", "Tag slug contains invalid characters. Only lowercase letters, numbers, and hyphens are allowed.", StatusCodes.Status400BadRequest);

    public static readonly Error InvalidColorHex =
        new("Tag.InvalidColorHex", "Color must be a valid hex code (e.g., #FF5733).", StatusCodes.Status400BadRequest);

    public static readonly Error CannotDeleteTagWithCourses =
        new("Tag.CannotDeleteWithCourses", "Cannot delete a tag that is assigned to courses. Remove the tag from all courses first or use force delete.", StatusCodes.Status400BadRequest);

    // ══════════════════════════════════════════════════════════════
    // Conflict (409)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error DuplicateName =
        new("Tag.DuplicateName", "A tag with this name already exists.", StatusCodes.Status409Conflict);

    public static readonly Error DuplicateSlug =
        new("Tag.DuplicateSlug", "A tag with this slug already exists.", StatusCodes.Status409Conflict);

    // ══════════════════════════════════════════════════════════════
    // Forbidden (403)
    // ══════════════════════════════════════════════════════════════

    public static readonly Error Unauthorized =
        new("Tag.Unauthorized", "You do not have permission to manage tags.", StatusCodes.Status403Forbidden);
}