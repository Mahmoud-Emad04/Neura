using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Courses;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Contracts.Section;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetCourseContent;

internal sealed class GetCourseContentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers) 
    : IRequestHandler<GetCourseContentQuery, Result<CourseResponse>>
{
    public async Task<Result<CourseResponse>> Handle(
        GetCourseContentQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var courseExists = await context.Courses.AsNoTracking()
            .AnyAsync(c => c.Id == courseId && !c.IsDeleted, ct);

        if (!courseExists)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var isEnrolled = !string.IsNullOrEmpty(request.UserId) && await context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu => cu.CourseId == courseId && cu.UserId == request.UserId && !cu.IsDeleted, ct);

        var sections = await context.Sections
            .AsNoTracking()
            .Where(s => s.CourseId == courseId && !s.IsDeleted)
            .OrderBy(s => s.Position)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Description,
                s.Position,
                Lessons = s.Lessons
                    .Where(l => l.Status == LessonStatus.Active && !l.IsDeleted)
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => new
                    {
                        l.Id,
                        l.Title,
                        l.Description,
                        l.Type,
                        l.Duration,
                        l.OrderIndex,
                        l.IsPreview,
                        Exam = l.Exam == null ? null : new
                        {
                            l.Id,
                            l.Exam.Title,
                            TotalQuestions = l.Exam.Questions.Count(),
                            l.Exam.DurationInMinutes,
                            l.Exam.PassingScorePercentage,
                            l.Exam.MaxAttempts
                        }
                    }).ToList()
            })
            .ToListAsync(ct);

        var completedLessonIds = new HashSet<int>();
        if (!string.IsNullOrEmpty(request.UserId))
        {
            completedLessonIds = await context.LessonCompletions
                .AsNoTracking()
                .Where(lc => lc.UserId == request.UserId && lc.Lesson.Section.CourseId == courseId)
                .Select(lc => lc.LessonId)
                .ToHashSetAsync(ct);
        }

        var sectionResponses = sections.Select(s => new SectionResponse(
            Id: s.Id,
            Title: s.Title,
            Description: s.Description,
            Position: s.Position,
            TotalMinutes: (int)s.Lessons.Sum(l => l.Duration.TotalMinutes),
            LessonsCount: s.Lessons.Count,
            Lessons: s.Lessons.Select(l => new LessonResponse(
                Id: l.Type == LessonType.Quiz && l.Exam != null ? l.Exam.Id : l.Id,
                Title: l.Type == LessonType.Quiz && l.Exam != null ? l.Exam.Title : l.Title,
                Description: l.Description,
                Type: l.Type.ToString(),
                Duration: l.Duration,
                OrderIndex: l.OrderIndex,
                IsPreview: l.IsPreview,
                IsLocked: !l.IsPreview && !isEnrolled,
                IsCompleted: completedLessonIds.Contains(l.Id),
                Exam: l.Exam == null ? null : new ExamPreviewInfo(
                    TotalQuestions: l.Exam.TotalQuestions,
                    DurationInMinutes: l.Exam.DurationInMinutes,
                    PassingScorePercentage: l.Exam.PassingScorePercentage,
                    MaxAttempts: l.Exam.MaxAttempts)
            )).ToList()
        )).ToList();

        var totalMinutes = sectionResponses.Sum(s => s.TotalMinutes);
        var totalLessons = sectionResponses.Sum(s => s.LessonsCount);

        var response = new CourseResponse(
            KeyId: request.CourseIdKey,
            TotalHours: (int)Math.Round(totalMinutes / 60.0),
            TotalLessons: totalLessons,
            Sections: sectionResponses);

        return Result.Success(response);
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
