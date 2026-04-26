using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class AnalyticsErrors
{
    public static readonly Error ExamNotFound =
        new("Analytics.ExamNotFound", "The specified exam was not found.", StatusCodes.Status404NotFound);

    public static readonly Error Forbidden =
        new("Analytics.Forbidden", "You do not have permission to view analytics for this exam.", StatusCodes.Status403Forbidden);

    public static readonly Error NoAttempts =
        new("Analytics.NoAttempts", "No attempts have been made for this exam yet.", StatusCodes.Status404NotFound);
}