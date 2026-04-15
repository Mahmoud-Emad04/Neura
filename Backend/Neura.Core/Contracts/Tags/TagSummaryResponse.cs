namespace Neura.Core.Contracts.Tags;

public sealed record TagSummaryResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public string? ColorHex { get; init; }
}