using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts.common;

namespace Neura.Core.Specifications.Courses;

public class BookmarkedCoursesFilterSpecification : BaseSpecification<CourseBookmark>
{
    public BookmarkedCoursesFilterSpecification(string userId, RequestFilters filters)
        : base(x => x.UserId == userId &&
                    !x.IsDeleted && !x.Course.IsDeleted &&
                    (string.IsNullOrEmpty(filters.SearchValue)
                     || x.Course.Title.Contains(filters.SearchValue)
                     || x.Course.Description.Contains(filters.SearchValue)
                     || (!string.IsNullOrEmpty(x.Course.DisplayInstructorName) &&
                         x.Course.DisplayInstructorName.Contains(filters.SearchValue)))
        )
    {
        AddInclude(c => c.Course);

        if (!string.IsNullOrEmpty(filters.SortColumn))
            AddOrderByString($"{filters.SortColumn} {filters.SortDirection}");
        else
            AddOrderByDescending(x => x.CreatedOn);
    }
}