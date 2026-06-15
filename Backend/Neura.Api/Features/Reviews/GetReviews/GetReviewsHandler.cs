using MediatR;
using Neura.Core.Contracts.Review;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Reviews.GetReviews;

internal sealed class GetReviewsHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetReviewsQuery, Result<PaginatedList<CourseFeedbackResponse>>>
{
    public async Task<Result<PaginatedList<CourseFeedbackResponse>>> Handle(
        GetReviewsQuery query, CancellationToken ct)
    {
        var keyId = query.CourseId;
        var pageNumber = query.PageNumber;
        var pageSize = query.PageSize;

        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
            return Result.Failure<PaginatedList<CourseFeedbackResponse>>(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

        var queryable = context.Reviews
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && !string.IsNullOrEmpty(r.Comment));

        var reviews = queryable
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedOn)
            .Select(r => new CourseFeedbackResponse(
                r.Comment!,
                $"{r.User.FirstName} {r.User.LastName}"
            ));

        var paginatedResult = await PaginatedList<CourseFeedbackResponse>.CreateAsync(
            reviews, pageNumber, pageSize, null, ct);

        return Result.Success(paginatedResult);
    }
}
