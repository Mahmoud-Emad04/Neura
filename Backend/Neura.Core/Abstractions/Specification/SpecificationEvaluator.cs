using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core; // <--- Critical for string sorting

namespace Neura.Core.Abstractions.Specification;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> spec) where TEntity : class
    {
        var query = inputQuery;

        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        // Apply Includes
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // --- SORTING LOGIC ---

        if (!string.IsNullOrWhiteSpace(spec.OrderByString))
        {
            query = query.OrderBy(spec.OrderByString);
        }
        else if (spec.OrderBy != null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        return query;
    }
}