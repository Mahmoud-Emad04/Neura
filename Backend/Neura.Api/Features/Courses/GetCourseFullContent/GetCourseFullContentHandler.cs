using System.Net;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;
using Neura.Core.Enums;
using Neura.Core.Errors;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetCourseFullContent;

internal sealed partial class GetCourseFullContentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers)
    : IRequestHandler<GetCourseFullContentQuery, Result<List<CourseFullContentResponse>>>
{
    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagsRegex();

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        var text = HtmlTagsRegex().Replace(html, string.Empty);
        return WebUtility.HtmlDecode(text).Trim();
    }
    public async Task<Result<List<CourseFullContentResponse>>> Handle(
        GetCourseFullContentQuery request, CancellationToken ct)
    {
        if (!TryDecodeCourseId(request.CourseIdKey, out var courseId))
            return Result.Failure<List<CourseFullContentResponse>>(CourseErrors.CourseNotFound);

        var course = await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.Title,
                LearningOutcomes = c.LearningOutcomes.Select(lo => lo.Outcome).ToList(),
                Prerequisites = c.Prerequisites.Select(p => p.Requirement).ToList(),
                Sections = c.Sections
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.Position)
                    .Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Description,
                        Lessons = s.Lessons
                            .Where(l => l.Status == LessonStatus.Active && !l.IsDeleted)
                            .OrderBy(l => l.OrderIndex)
                            .Select(l => new
                            {
                                l.Id,
                                l.Title,
                                l.Description,
                                l.Type,
                                l.ArticleContent
                            }).ToList()
                    }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (course is null)
            return Result.Failure<List<CourseFullContentResponse>>(CourseErrors.CourseNotFound);

        var response = new CourseFullContentResponse(
            CourseId: course.Id,
            CourseTitle: course.Title,
            LearningOutcomes: course.LearningOutcomes,
            Prerequisites: course.Prerequisites,
            Sections: course.Sections.Select(s => new CourseFullContentSectionResponse(
                SectionId: s.Id,
                SectionTitle: s.Title,
                SectionDescription: s.Description,
                Lessons: s.Lessons.Select(l => new CourseFullContentLessonResponse(
                    LessonId: l.Id,
                    LessonTitle: l.Title,
                    LessonDescription: l.Description,
                    LessonText: l.Type == LessonType.Article ? StripHtml(l.ArticleContent) : null
                )).ToList()
            )).ToList()
        );

        return Result.Success(new List<CourseFullContentResponse> { response });
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
