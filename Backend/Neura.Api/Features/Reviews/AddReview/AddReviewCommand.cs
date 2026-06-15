using MediatR;
using Neura.Core.Contracts.Review;

namespace Neura.Api.Features.Reviews.AddReview;

public sealed record AddReviewCommand(string CourseId, ReviewRequest Request, string UserId)
    : IRequest<Result>;
