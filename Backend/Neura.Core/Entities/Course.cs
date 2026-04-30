using Neura.Core.Abstractions;
using Neura.Core.Enums;
using Neura.Core.Errors;

namespace Neura.Core.Entities;

public sealed class Course : AuditableEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? DisplayInstructorName { get; set; }
    public string Description { get; set; } = string.Empty;

    public CourseStatus Status { get; set; } = CourseStatus.Pending;

    public string ImageUrl { get; set; } = string.Empty;
    public int Price { get; set; }

    public double Rating { get; private set; }
    public double TotalRatingSum { get; private set; }
    public int TotalReviews { get; private set; }

    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Section> Sections { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<CourseUser> CourseUsers { get; set; } = [];
    public ICollection<CourseLearningOutcome> LearningOutcomes { get; set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];

    /// <summary>
    ///     Checks if the course is currently accepting new enrollments.
    /// </summary>
    public bool IsEnrollmentOpen => Status == CourseStatus.Active && !IsDeleted;

    /// <summary>
    ///     Checks if enrolled students can access course content.
    /// </summary>
    public bool IsAccessibleToStudents => Status is CourseStatus.Active or CourseStatus.Completed && !IsDeleted;


    // ══════════════════════════════════════════════════════════════
    // Domain Methods
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    ///     Activates a pending course, making it available for enrollment.
    /// </summary>
    public Result Activate()
    {
        if (Status != CourseStatus.Pending)
            return Result.Failure(CourseErrors.CanOnlyActivateFromPending);

        Status = CourseStatus.Active;
        return Result.Success();
    }

    /// <summary>
    ///     Transitions course from Active to Completed.
    ///     Enrolled students retain access, but no new enrollments allowed.
    /// </summary>
    public Result Complete()
    {
        if (Status != CourseStatus.Active)
            return Result.Failure(CourseErrors.CanOnlyCompleteFromActive);

        Status = CourseStatus.Completed;
        return Result.Success();
    }

    /// <summary>
    ///     Reactivates a completed course (e.g., instructor wants to run it again).
    /// </summary>
    public Result Reactivate()
    {
        if (Status != CourseStatus.Completed)
            return Result.Failure(CourseErrors.CanOnlyReactivateFromCompleted);

        Status = CourseStatus.Active;
        return Result.Success();
    }

    /// <summary>
    ///     Moves course back to Pending (e.g., for major revisions).
    ///     Only allowed when course is Active.
    /// </summary>
    public Result Unpublish()
    {
        if (Status != CourseStatus.Active)
            return Result.Failure(CourseErrors.CanOnlyUnpublishFromActive);

        Status = CourseStatus.Pending;
        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════════
    // Rating Domain Methods
    // ══════════════════════════════════════════════════════════════

    public void AddReview(int starRating)
    {
        if (starRating < 1 || starRating > 5)
            throw new ArgumentOutOfRangeException(nameof(starRating), "Rating must be between 1 and 5.");

        TotalRatingSum += starRating;
        TotalReviews++;
        RecalculateRating();
    }

    public void RemoveReview(int starRating)
    {
        TotalRatingSum -= starRating;
        TotalReviews = Math.Max(0, TotalReviews - 1);
        RecalculateRating();
    }

    public void UpdateReview(int oldRating, int newRating)
    {
        TotalRatingSum = TotalRatingSum - oldRating + newRating;
        RecalculateRating();
    }

    private void RecalculateRating()
    {
        Rating = TotalReviews == 0 ? 0 : Math.Round(TotalRatingSum / TotalReviews, 2);
    }
}