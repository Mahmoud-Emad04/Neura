using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class SectionErrors
{
    public static readonly Error SectionNotFound =
        new("Section.NotFound", "The specified Section was not found.", StatusCodes.Status404NotFound);

    public static readonly Error SectionInvalidData =
        new("Section.InvalidData", "One or more section fields are invalid.", StatusCodes.Status400BadRequest);

    public static readonly Error SectionPositionConflict =
        new("Section.PositionConflict", "Another section in this course already uses the same position.",
            StatusCodes.Status409Conflict);

    public static readonly Error SectionAlreadyDeleted =
        new("Section.AlreadyDeleted", "The specified section is already deleted.", StatusCodes.Status409Conflict);
}