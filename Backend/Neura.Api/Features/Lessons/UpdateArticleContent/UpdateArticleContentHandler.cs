using Ganss.Xss;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.UpdateArticleContent;

internal sealed class UpdateArticleContentHandler(
    ApplicationDbContext context,
    HtmlSanitizer sanitizer) 
    : IRequestHandler<UpdateArticleContentCommand, Result>
{
    public async Task<Result> Handle(
        UpdateArticleContentCommand command, CancellationToken ct)
    {
        var lessonId = command.LessonId;
        var request = command.Request;

        var lesson = await context.Lessons
            .Include(l => l.Section)
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted, ct);

        if (lesson is null)
            return Result.Failure(LessonErrors.NotFound);

        if (lesson.Type != LessonType.Article)
            return Result.Failure(new Error("Lesson.InvalidType", "Content can only be added to Article-type lessons.", StatusCodes.Status400BadRequest));

        if (lesson.Status == LessonStatus.Draft)
            lesson.Status = LessonStatus.Active;

        var cleanHtml = sanitizer.Sanitize(request.HtmlContent);

        lesson.ArticleContent = cleanHtml;
        lesson.IsPublished = true;
        lesson.UpdatedOn = DateTime.UtcNow;
        lesson.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
