using MediatR;
using Neura.Core.Contracts.Tags;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.UpdateTag;

internal sealed class UpdateTagHandler(ApplicationDbContext context)
    : IRequestHandler<UpdateTagCommand, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(
        UpdateTagCommand command, CancellationToken ct)
    {
        var id = command.Id;
        var request = command.Request;
        var userId = command.UserId;

        var tag = await context.Tags
            .Include(t => t.Courses)
            .SingleOrDefaultAsync(t => t.Id == id, ct);

        if (tag is null)
            return Result.Failure<TagResponse>(TagErrors.TagNotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            return Result.Failure<TagResponse>(TagErrors.InvalidName);

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? TagHelpers.GenerateSlug(name)
            : TagHelpers.NormalizeSlug(request.Slug);

        if (!TagHelpers.IsValidSlug(slug))
            return Result.Failure<TagResponse>(TagErrors.InvalidSlug);

        if (!string.IsNullOrWhiteSpace(request.ColorHex) && !TagHelpers.IsValidHexColor(request.ColorHex))
            return Result.Failure<TagResponse>(TagErrors.InvalidColorHex);

        var existingByName = await context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id != id && t.Name.ToLower() == name.ToLower(), ct);

        if (existingByName)
            return Result.Failure<TagResponse>(TagErrors.DuplicateName);

        var existingBySlug = await context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Id != id && t.Slug == slug, ct);

        if (existingBySlug)
            return Result.Failure<TagResponse>(TagErrors.DuplicateSlug);

        tag.Name = name;
        tag.Description = request.Description?.Trim();
        tag.Slug = slug;
        tag.IconUrl = request.IconUrl?.Trim();
        tag.ColorHex = TagHelpers.NormalizeHexColor(request.ColorHex);
        tag.DisplayOrder = request.DisplayOrder;
        tag.IsActive = request.IsActive;
        tag.UpdatedOn = DateTime.UtcNow;
        tag.UpdatedById = userId;

        await context.SaveChangesAsync(ct);

        return Result.Success(new TagResponse
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            Slug = tag.Slug,
            IconUrl = tag.IconUrl,
            ColorHex = tag.ColorHex,
            DisplayOrder = tag.DisplayOrder,
            IsActive = tag.IsActive,
            CourseCount = tag.Courses.Count(c => !c.IsDeleted),
            CreatedOn = tag.CreatedOn,
            UpdatedOn = tag.UpdatedOn
        });
    }
}
