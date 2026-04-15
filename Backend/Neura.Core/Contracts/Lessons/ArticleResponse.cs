namespace Neura.Core.Contracts.Lessons;

public record ArticleResponse(
    int LessonId,
    string Title,
    string HtmlContent
);