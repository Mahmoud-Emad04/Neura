using MediatR;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.BulkUpdateTagsOrder;

internal sealed class BulkUpdateTagsOrderHandler(ApplicationDbContext context)
    : IRequestHandler<BulkUpdateTagsOrderCommand, Result>
{
    public async Task<Result> Handle(
        BulkUpdateTagsOrderCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (request.Tags.Count == 0)
            return Result.Success();

        var tagIds = request.Tags.Select(t => t.Id).ToList();

        var tags = await context.Tags
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != tagIds.Count)
            return Result.Failure(TagErrors.TagsNotFound);

        var orderMap = request.Tags.ToDictionary(t => t.Id, t => t.DisplayOrder);

        foreach (var tag in tags)
        {
            tag.DisplayOrder = orderMap[tag.Id];
            tag.UpdatedOn = DateTime.UtcNow;
            tag.UpdatedById = command.UserId;
        }

        await context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
