using MediatR;
using Neura.Core.Contracts.Review;

namespace Neura.Api.Features.Reviews.GetReviews;

public sealed record GetReviewsQuery(string CourseId, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<CourseFeedbackResponse>>>;
