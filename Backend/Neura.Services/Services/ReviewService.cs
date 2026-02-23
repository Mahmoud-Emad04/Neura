using Neura.Core.Contracts.Review;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class ReviewService(ApplicationDbContext context, IServiceHelpers helpers) : IReviewService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceHelpers _helpers = helpers;

    public async Task<Result<PaginatedList<CourseFeedbackResponse>>> CourseReviewsAsync(string keyId,
        int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
            return Result.Failure<PaginatedList<CourseFeedbackResponse>>(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && !string.IsNullOrEmpty(r.Comment));

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = query
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedOn)
            .Select(r => new CourseFeedbackResponse(
                r.Comment!,
                $"{r.User.FirstName} {r.User.LastName}"
            ));
        var paginatedResult =
            await PaginatedList<CourseFeedbackResponse>.CreateAsync(reviews, pageNumber, pageSize, null,
                cancellationToken);

        return Result.Success(paginatedResult);
    }
}