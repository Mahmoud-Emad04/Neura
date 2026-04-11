using Neura.Core.Contracts.Review;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class ReviewService(ApplicationDbContext context, IServiceHelpers helpers) : IReviewService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceHelpers _helpers = helpers;

    public async Task<Result> AddReviewAsync(string keyId, string userId, ReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return Result.Failure(ReviewErrors.InvalidRating);

        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var courseMeta = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.CreatedById, c.IsDeleted })
            .FirstOrDefaultAsync(cancellationToken);

        if (courseMeta is null || courseMeta.IsDeleted)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (courseMeta.CreatedById == userId)
            return Result.Failure(ReviewErrors.CannotReviewOwnCourse);

        var isEnrolled = await _context.CourseUsers
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.CourseId == courseId && !c.IsDeleted, cancellationToken);

        if (!isEnrolled)
            return Result.Failure(ReviewErrors.NotEnrolled);

        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId, cancellationToken);

        if (existingReview is not null)
        {
            existingReview.Rating = request.Rating;
            existingReview.Comment = request.Comment;
            existingReview.UpdatedOn = DateTime.UtcNow;
            _context.Reviews.Update(existingReview);
        }
        else
        {
            var review = new Review
            {
                CourseId = courseId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedOn = DateTime.UtcNow
            };
            await _context.Reviews.AddAsync(review, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var stats = await _context.Reviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.CourseId)
            .Select(g => new
            {
                Count = g.Count(),
                Average = g.Average(r => r.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats is not null)
            await _context.Courses
                .Where(c => c.Id == courseId)
                .ExecuteUpdateAsync(calls => calls
                        .SetProperty(c => c.TotalReviews, stats.Count)
                        .SetProperty(c => c.Rating, Math.Round(stats.Average, 1)),
                    cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PaginatedList<CourseFeedbackResponse>>> CourseReviewsAsync(string keyId,
        int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<PaginatedList<CourseFeedbackResponse>>(CourseErrors.CourseNotFound);

        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && !string.IsNullOrEmpty(r.Comment));

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

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }

        courseId = numbers[0];
        return true;
    }
}