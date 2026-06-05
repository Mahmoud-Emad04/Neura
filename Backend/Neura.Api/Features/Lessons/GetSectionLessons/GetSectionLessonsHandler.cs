using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.GetSectionLessons;

internal sealed class GetSectionLessonsHandler(
    ApplicationDbContext context) 
    : IRequestHandler<GetSectionLessonsQuery, Result<List<LessonWithPositionResponse>>>
{
    public async Task<Result<List<LessonWithPositionResponse>>> Handle(
        GetSectionLessonsQuery query, CancellationToken ct)
    {
        var section = await context.Sections
            .Include(s => s.Lessons.Where(l => !l.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == query.SectionId && !s.IsDeleted, ct);

        if (section is null)
            return Result.Failure<List<LessonWithPositionResponse>>(SectionErrors.SectionNotFound);

        var totalLessons = section.Lessons.Count;
        var lessons = section.Lessons
            .OrderBy(l => l.OrderIndex)
            .Select(l => new LessonWithPositionResponse(
                l.Id,
                l.Title,
                l.Description,
                l.OrderIndex,
                totalLessons,
                l.IsPreview,
                l.IsVideoPrivate,
                l.IsPublished,
                l.Type,
                l.CloudinaryVideoUrl,
                l.UpdatedOn ?? l.CreatedOn
            ))
            .ToList();

        return Result.Success(lessons);
    }
}
