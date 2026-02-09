using Microsoft.AspNetCore.Hosting;
using Neura.Core.Contracts.Lessons;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class LessonService(ApplicationDbContext context, IFileService fileService, IWebHostEnvironment webHostEnvironment) : ILessonService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly string _filesPath = $"{webHostEnvironment.WebRootPath}/Files";
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    public async Task<Result<int>> CreateLessonMetadataAsync(CreateLessonRequest request, CancellationToken cancellationToken)
    {

        var validSection = await _context.Sections.AnyAsync(s => s.Id == request.SectionId, cancellationToken);

        if (!validSection)
            return Result.Failure<int>(SectionErrors.SectionNotFound);

        var lastOrder = await _context.Lessons
                    .Where(l => l.SectionId == request.SectionId)
                    .MaxAsync(l => (int?)l.OrderIndex, cancellationToken) ?? 0;

        var lesson = new Lesson
        {
            Title = request.Title,
            SectionId = request.SectionId,
            Type = request.Type,
            OrderIndex = lastOrder + 1,
            IsPublished = false
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(lesson.Id);
    }

    public async Task<Result> CompleteLessonAsync(int id, CompleteLessonRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Lessons.FindAsync(id, cancellationToken) is not { } lesson)
            return Result.Failure(LessonErrors.NotFound);

        if (request.VideoFile is not null && lesson.Type == LessonType.Video)
        {
            var sortedName = await _fileService.UploadAsync(request.VideoFile, "Lessons", cancellationToken);
            lesson.VideoSortedName = sortedName;
        }

        lesson.Description = request.Description;
        lesson.IsPreview = request.IsPreview;
        lesson.IsPublished = true;

        if (request.ScheduledDate.HasValue)
        {
            lesson.ScheduledDate = request.ScheduledDate.Value;
        }
        else
        {
            lesson.ScheduledDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<LessonResponse>> GetLessonByIdAsync(int lessonId, string userId, CancellationToken ct)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null) return Result.Failure<LessonResponse>(LessonErrors.NotFound);

        var isInstructor = lesson.Section.Course.CreatedById == userId;
        var canView = isInstructor || lesson.IsPreview;

        if (!canView) return Result.Failure<LessonResponse>(LessonErrors.NotEnrolled);

        string? streamUrl = null;

        if (lesson.Type == LessonType.Video && lesson.VideoSortedName is not null)
        {
            // This points to the controller method we wrote earlier: [HttpGet("{id}/stream")]
            // We abstract the physical location. The ID is all the endpoint needs.
            streamUrl = $"/api/lessons/{lesson.Id}/stream";
        }

        var response = lesson.Adapt<LessonResponse>() with { VideoUrl = streamUrl };
        return Result.Success(response);
    }

    public async Task<Result<(string Path, string ContentType)>> GetLessonVideoPathAsync(
        int lessonId, string userId, CancellationToken ct)
    {
        // 1. Fetch Lesson & Security (Keep your existing logic)
        var lesson = await context.Lessons
            .AsNoTracking()
            .Include(l => l.Section.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson is null) return Result.Failure<(string, string)>(LessonErrors.NotFound);

        // Security Check
        var isInstructor = lesson.Section.Course.CreatedById == userId;
        var canView = isInstructor || lesson.IsPreview;

        //if (!canView) return Result.Failure<(string, string)>(LessonErrors.NotEnrolled);

        if (!lesson.VideoSortedName.HasValue)
            return Result.Failure<(string, string)>(LessonErrors.VideoNotFound);

        // 2. Get PATH from FileService (New Method) 🚀
        var fileData = await _fileService.GetFilePathAsync(lesson.VideoSortedName.Value, "Lessons", ct);

        if (fileData.path is null)
            return Result.Failure<(string, string)>(LessonErrors.FileNotFound);

        return Result.Success((fileData.path, fileData.contentType));
    }
}
