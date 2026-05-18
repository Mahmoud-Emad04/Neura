using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Tags;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Tags.CreateTag;

internal sealed class CreateTagHandler(ApplicationDbContext context) 
    : IRequestHandler<CreateTagCommand, Result<TagResponse>>
{
    public async Task<Result<TagResponse>> Handle(
        CreateTagCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var userId = command.UserId;

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
            .AnyAsync(t => t.Name.ToLower() == name.ToLower(), ct);

        if (existingByName)
            return Result.Failure<TagResponse>(TagErrors.DuplicateName);

        var existingBySlug = await context.Tags
            .AsNoTracking()
            .AnyAsync(t => t.Slug == slug, ct);

        if (existingBySlug)
            return Result.Failure<TagResponse>(TagErrors.DuplicateSlug);

        var displayOrder = request.DisplayOrder;
        if (displayOrder == 0)
        {
            displayOrder = await context.Tags
                .AsNoTracking()
                .MaxAsync(t => (int?)t.DisplayOrder, ct) ?? 0;
            displayOrder++;
        }

        var tag = new Tag
        {
            Name = name,
            Description = request.Description?.Trim(),
            Slug = slug,
            IconUrl = request.IconUrl?.Trim(),
            ColorHex = TagHelpers.NormalizeHexColor(request.ColorHex),
            DisplayOrder = displayOrder,
            IsActive = request.IsActive,
            CreatedOn = DateTime.UtcNow,
            CreatedById = userId
        };

        context.Tags.Add(tag);
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
            CourseCount = 0,
            CreatedOn = tag.CreatedOn,
            UpdatedOn = tag.UpdatedOn
        });
    }
}
