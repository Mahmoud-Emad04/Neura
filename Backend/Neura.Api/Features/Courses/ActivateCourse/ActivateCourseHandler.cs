using MediatR;
using Neura.Core.Contracts.Courses;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.ActivateCourse;

internal sealed class ActivateCourseHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<ActivateCourseCommand, Result<CourseStatusUpdateResponse>>
{
    public async Task<Result<CourseStatusUpdateResponse>> Handle(
        ActivateCourseCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Activate();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = command.UserId;

        await context.SaveChangesAsync(ct);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = command.CourseIdKey,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course activated successfully. Students can now enroll.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }
        courseId = numbers[0];
        return true;
    }
}
