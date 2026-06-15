using System.Net;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Neura.Core.Abstractions;
using Neura.Core.Contracts.Course;
using Neura.Core.Enums;
using Neura.Repository.Persistence;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Courses.GetCourseFullContent;

internal sealed partial class GetCourseFullContentHandler(
    ApplicationDbContext context,
    IServiceHelpers helpers,
    Microsoft.Extensions.Caching.Hybrid.HybridCache hybridCache)
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
        var response = await hybridCache.GetOrCreateAsync(
            "course-full-content",
            async cancel =>
            {
                var courses = await context.Courses
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
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
                    .ToListAsync(cancel);

                return courses.Select(course => new CourseFullContentResponse(
                    CourseId: helpers.Encode(course.Id),
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
                )).ToList();
            },
            cancellationToken: ct
        );

        return Result.Success(response);
    }
}
