using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts.common;
using Neura.Core.Entities;

namespace Neura.Core.Specifications.Courses;
// Adjusted namespace to cleaner structure

public class CourseFilterSpecification : BaseSpecification<Course>
{
    public CourseFilterSpecification(RequestFilters filters)
        : base(x =>
            !x.IsDeleted &&
            (string.IsNullOrEmpty(filters.SearchValue) || x.Title.Contains(filters.SearchValue) ||
             x.Description.Contains(filters.SearchValue)) &&
            (!filters.IsFree.HasValue || (filters.IsFree.Value ? x.Price == 0 : x.Price > 0))
        )
    {
        AddInclude(x => x.Tags);

        if (!string.IsNullOrEmpty(filters.SortColumn))
            AddOrderByString($"{filters.SortColumn} {filters.SortDirection}");
        else
            AddOrderByDescending(x => x.CreatedOn);
    }
}