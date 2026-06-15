using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.BulkToggleTagsActive;

internal sealed class BulkToggleTagsActiveHandler(ApplicationDbContext context)
    : IRequestHandler<BulkToggleTagsActiveCommand, Result>
{
    public async Task<Result> Handle(
        BulkToggleTagsActiveCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.TagIds.Count == 0)
            return Result.Success();

        var tags = await context.Tags
            .Where(t => request.TagIds.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != request.TagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        foreach (var tag in tags)
        {
            tag.IsActive = request.IsActive;
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = command.UserId;
        }

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
