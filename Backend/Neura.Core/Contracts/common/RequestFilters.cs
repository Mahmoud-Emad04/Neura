using Neura.Core.Enums;

namespace Neura.Core.Contracts.common;

public record RequestFilters
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchValue { get; init; }
    public string? SortColumn { get; init; }
    public string? SortDirection { get; init; } = "ASC";
    public bool? IsFree { get; set; }
    public bool? IsBookmarked { get; set; }
    public CourseStatus? CourseStatus { get; set; }
}