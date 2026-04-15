namespace Neura.Core.Contracts.Tags;

public sealed record CreateTagRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Slug { get; init; }  // Auto-generated if not provided
    public string? IconUrl { get; init; }
    public string? ColorHex { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
}