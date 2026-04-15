using Neura.Core.Abstractions;

namespace Neura.Core.Contracts.Tags;

public sealed record TagListResponse
{
    public int TotalTags { get; init; }
    public int ActiveTags { get; init; }
    public int InactiveTags { get; init; }
    public PaginatedList<TagResponse> Tags { get; init; } = default!;
}