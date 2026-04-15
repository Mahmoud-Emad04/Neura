namespace Neura.Core.Contracts.Tags;

public sealed record TagFilters
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public string SortBy { get; init; } = "DisplayOrder";
    public bool SortDescending { get; init; } = false;
    public bool IncludeCourseCount { get; init; } = true;
}