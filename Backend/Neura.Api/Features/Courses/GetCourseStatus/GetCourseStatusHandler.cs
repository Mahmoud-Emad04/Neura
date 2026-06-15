using MediatR;
using Neura.Core.Contracts.Courses;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetCourseStatus;

internal sealed class GetCourseStatusHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetCourseStatusQuery, Result<CourseStatusResponse>>
{
    public async Task<Result<CourseStatusResponse>> Handle(
        GetCourseStatusQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        ActivationRequirements? requirements = null;

        if (course.Status == CourseStatus.Pending)
            requirements = await GetActivationRequirementsAsync(courseId, ct);

        var response = new CourseStatusResponse
        {
            KeyId = request.CourseIdKey,
            Status = course.Status,
            StatusName = course.Status.ToString(),
            IsEnrollmentOpen = course.IsEnrollmentOpen,
            IsAccessibleToStudents = course.IsAccessibleToStudents,
            CanActivate = course.Status == CourseStatus.Pending,
            CanComplete = course.Status == CourseStatus.Active,
            CanReactivate = course.Status == CourseStatus.Completed,
            CanUnpublish = course.Status == CourseStatus.Active,
            Requirements = requirements
        };

        return Result.Success(response);
    }

    private async Task<ActivationRequirements> GetActivationRequirementsAsync(
        int courseId,
        CancellationToken ct)
    {
        var stats = await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                TotalSections = c.Sections.Count(s => !s.IsDeleted),
                TotalLessons = c.Sections
                    .Where(s => !s.IsDeleted)
                    .SelectMany(s => s.Lessons)
                    .Count(l => !l.IsDeleted),
                PublishedLessons = c.Sections
                    .Where(s => !s.IsDeleted)
                    .SelectMany(s => s.Lessons)
                    .Count(l => !l.IsDeleted && l.IsPublished)
            })
            .SingleOrDefaultAsync(ct);

        var missingRequirements = new List<string>();

        if (stats?.TotalSections == 0)
            missingRequirements.Add("Course must have at least one section.");

        if (stats?.TotalLessons == 0)
            missingRequirements.Add("Course must have at least one lesson.");

        if (stats?.PublishedLessons == 0)
            missingRequirements.Add("Course must have at least one published lesson.");

        return new ActivationRequirements
        {
            HasSections = stats?.TotalSections > 0,
            HasLessons = stats?.TotalLessons > 0,
            HasPublishedLessons = stats?.PublishedLessons > 0,
            TotalSections = stats?.TotalSections ?? 0,
            TotalLessons = stats?.TotalLessons ?? 0,
            PublishedLessons = stats?.PublishedLessons ?? 0,
            MissingRequirements = missingRequirements
        };
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
