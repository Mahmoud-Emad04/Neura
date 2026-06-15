using MediatR;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.GetArticleContent;

internal sealed class GetArticleContentHandler(
    ApplicationDbContext context)
    : IRequestHandler<GetArticleContentQuery, Result<ArticleResponse>>
{
    public async Task<Result<ArticleResponse>> Handle(
        GetArticleContentQuery query, CancellationToken ct)
    {
        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == query.LessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure<ArticleResponse>(LessonErrors.NotFound);

        if (lesson.Type != LessonType.Article)
            return Result.Failure<ArticleResponse>(
                new Error("Lesson.InvalidType", "This lesson is not an article.", StatusCodes.Status400BadRequest));

        var response = new ArticleResponse(
            lesson.Id,
            lesson.Title,
            lesson.ArticleContent ?? string.Empty
        );

        return Result.Success(response);
    }
}
