using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Review;
using Neura.Core.Entities;
using Neura.Core.Errors;
using Neura.Services.Helpers;
using Neura.Repository.Persistence;

namespace Neura.Api.Features.Reviews.AddReview;

internal sealed class AddReviewHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<AddReviewCommand, Result>
{
    public async Task<Result> Handle(
        AddReviewCommand command, CancellationToken ct)
    {
        var keyId = command.CourseId;
        var request = command.Request;
        var userId = command.UserId;

        if (request.Rating < 1 || request.Rating > 5)
            return Result.Failure(ReviewErrors.InvalidRating);

        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

        var courseMeta = await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new { c.CreatedById, c.IsDeleted })
            .FirstOrDefaultAsync(ct);

        if (courseMeta is null || courseMeta.IsDeleted)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (courseMeta.CreatedById == userId)
            return Result.Failure(ReviewErrors.CannotReviewOwnCourse);

        var isEnrolled = await context.CourseUsers
            .AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.CourseId == courseId && !c.IsDeleted, ct);

        if (!isEnrolled)
            return Result.Failure(ReviewErrors.NotEnrolled);

        var existingReview = await context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId, ct);

        if (existingReview is not null)
        {
            existingReview.Rating = request.Rating;
            existingReview.Comment = request.Comment;
            existingReview.UpdatedOn = DateTime.UtcNow;
            context.Reviews.Update(existingReview);
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
            await context.Reviews.AddAsync(review, ct);
        }

        await context.SaveChangesAsync(ct);

        var stats = await context.Reviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.CourseId)
            .Select(g => new
            {
                Count = g.Count(),
                Average = g.Average(r => r.Rating)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is not null)
        {
            await context.Courses
                .Where(c => c.Id == courseId)
                .ExecuteUpdateAsync(calls => calls
                        .SetProperty(c => c.TotalReviews, stats.Count)
                        .SetProperty(c => c.Rating, Math.Round(stats.Average, 1)),
                    ct);
        }

        return Result.Success();
    }
}
