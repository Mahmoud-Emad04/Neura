using Microsoft.EntityFrameworkCore;

namespace Neura.Core.Abstractions;

public class PaginatedList<T>(List<T> items, int pageNumber, int count, int pageSize)
{
    public List<T> Items { get; private set; } = items;
    public int PageNumber { get; } = pageNumber;
    public int TotalPages { get; } = (count + pageSize - 1) / pageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize,
        Action<T>? transform = null, CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        if (transform != null) items.ForEach(transform);
        return new PaginatedList<T>(items, pageNumber, count, pageSize);
    }
}