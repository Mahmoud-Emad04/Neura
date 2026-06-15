using MediatR;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Lessons.CreateLessonMetadata;

internal sealed class CreateLessonMetadataHandler(
    ApplicationDbContext context)
    : IRequestHandler<CreateLessonMetadataCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        CreateLessonMetadataCommand command, CancellationToken ct)
    {
        var sectionId = command.SectionId;
        var request = command.Request;

        if (sectionId < 1)
            return Result.Failure<int>(new Error("InvalidSectionId", "The provided section ID is invalid.", StatusCodes.Status400BadRequest));

        var section = await context.Sections
            .AsNoTracking()
            .Select(s => new { s.Id, s.CourseId })
            .FirstOrDefaultAsync(s => s.Id == sectionId, ct);

        if (section is null)
            return Result.Failure<int>(SectionErrors.SectionNotFound);

        //var positionConflict = await context.Lessons
        //    .AnyAsync(l => l.SectionId == sectionId && l.OrderIndex == request.Position, ct);

        //if (positionConflict)
        //    return Result.Failure<int>(LessonErrors.LessonPositionConflict);

        var lastOrder = await context.Lessons
            .Where(l => l.SectionId == sectionId)
            .MaxAsync(l => (int?)l.OrderIndex, ct) ?? 0;

        var lesson = new Lesson
        {
            Title = request.Title,
            SectionId = sectionId,
            Type = request.Type,
            OrderIndex = lastOrder + 1,
            IsPublished = true,
            IsVideoPrivate = false,
            Status = LessonStatus.Draft,
            CreatedById = command.UserId,
            CreatedOn = DateTime.UtcNow
        };

        context.Lessons.Add(lesson);
        await context.SaveChangesAsync(ct);

        return Result.Success(lesson.Id);
    }
}
