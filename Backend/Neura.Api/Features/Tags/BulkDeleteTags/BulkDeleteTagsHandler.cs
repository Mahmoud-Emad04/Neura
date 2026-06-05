using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.BulkDeleteTags;

internal sealed class BulkDeleteTagsHandler(ApplicationDbContext context) 
    : IRequestHandler<BulkDeleteTagsCommand, Result>
{
    public async Task<Result> Handle(
        BulkDeleteTagsCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.TagIds.Count == 0)
            return Result.Success();

        var tags = await context.Tags
            .Include(t => t.Courses)
            .Where(t => request.TagIds.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != request.TagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        if (!command.Force)
        {
            var tagsWithCourses = tags.Where(t => t.Courses.Any(c => !c.IsDeleted)).ToList();
            if (tagsWithCourses.Count > 0)
                return Result.Failure(TagErrors.CannotDeleteTagWithCourses);
        }

        foreach (var tag in tags)
        {
            if (command.Force) tag.Courses.Clear();

            tag.IsDeleted = true;
            tag.IsActive = false;
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = command.UserId;
        }

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
