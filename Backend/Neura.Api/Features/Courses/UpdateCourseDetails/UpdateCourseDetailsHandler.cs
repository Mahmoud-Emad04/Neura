using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Neura.Api.Infrastructure;
using Neura.Core.Errors;
using Neura.Core.FilesConsts;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.UpdateCourseDetails;

internal sealed class UpdateCourseDetailsHandler(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers,
    HybridCache hybridCache)
    : IRequestHandler<UpdateCourseDetailsCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCourseDetailsCommand command, CancellationToken ct)
    {
        if (!TryDecodeCourseId(command.CourseIdKey, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var request = command.Request;
        var userId = command.UserId;

        var course = await context.Courses
            .Include(c => c.Tags)
            .Include(c => c.LearningOutcomes)
            .Include(c => c.Prerequisites)
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        var tags = await context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(ct);

        if (tags.Count != request.Tags.Count)
            return Result.Failure(CourseErrors.TagNotFound);

        course.Title = request.Title.Trim();
        course.Description = request.Description?.Trim() ?? string.Empty;
        course.Price = request.Price;
        course.DisplayInstructorName = request.InstructorName?.Trim();
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        course.Tags.Clear();
        foreach (var tag in tags) course.Tags.Add(tag);

        context.CourseLearningOutcomes.RemoveRange(course.LearningOutcomes);

        course.LearningOutcomes = request.LearningOutcomes
            .Where(lo => !string.IsNullOrWhiteSpace(lo))
            .Select(outcome => new CourseLearningOutcome
            {
                CourseId = courseId,
                Outcome = outcome.Trim()
            })
            .ToList();

        context.CoursePrerequisites.RemoveRange(course.Prerequisites);

        course.Prerequisites = request.Prerequisites
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(prereq => new CoursePrerequisite
            {
                CourseId = courseId,
                Requirement = prereq.Trim()
            })
            .ToList();

        if (request.Image is not null)
        {
            var defaultPath = Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
            if (!string.IsNullOrEmpty(course.ImageUrl) && course.ImageUrl != defaultPath)
            {
                fileService.Delete(course.ImageUrl);
            }

            course.ImageUrl = await fileService.UploadImageAsync(
                request.Image,
                ImageConsts.Course,
                ct);
        }

        await context.SaveChangesAsync(ct);

        // Invalidate course caches
        await hybridCache.RemoveAsync(CacheKeys.CourseFullContent, ct);
        return Result.Success();
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
