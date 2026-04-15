namespace Neura.Core.Contracts.Tags;

public sealed record BulkUpdateTagsOrderRequest
{
    public List<TagOrderItem> Tags { get; init; } = [];
}

public sealed record TagOrderItem
{
    public int Id { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record BulkToggleTagsActiveRequest
{
    public List<int> TagIds { get; init; } = [];
    public bool IsActive { get; init; }
}

public sealed record BulkDeleteTagsRequest
{
    public List<int> TagIds { get; init; } = [];
}