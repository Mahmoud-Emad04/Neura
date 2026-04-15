namespace Neura.Core.Contracts.Tags;

public sealed record TagResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Slug { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? ColorHex { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public int CourseCount { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? UpdatedOn { get; init; }
}