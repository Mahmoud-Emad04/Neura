using Neura.Core.Abstractions;
using Neura.Core.Contracts.Review;

namespace Neura.Core.Services;

public interface IReviewService
{
    Task<Result<PaginatedList<CourseFeedbackResponse>>> CourseReviewsAsync(string keyId, int pageNumber = 1,
        int pageSize = 5, CancellationToken cancellationToken = default);
}