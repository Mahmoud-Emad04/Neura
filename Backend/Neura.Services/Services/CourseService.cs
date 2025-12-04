
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Neura.Core.Contracts.Files;
using System.Linq;

namespace Neura.Services.Services;

public class CourseService(ApplicationDbContext context, IFileService fileService, IHttpContextAccessor httpContextAccessor) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly Hashids _hashids = new("Course", 8);
    private readonly string _folderName = "Course";
    public async Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _context.Courses
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
            course.ImageUrl = Path.Combine(GetBaseUrl(), course.ImageUrl);

        var response = courses.Adapt<IEnumerable<CourseResponse>>();

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        int[] numbers = _hashids.Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(GetBaseUrl(), course.ImageUrl) };

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> CreateAsync(CourseRequest request, UploadImageRequest uploadImage, string userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
                        .Where(t => request.Tags.Contains(t.Id))
                        .ToListAsync(cancellationToken);

        if (tags.Count() != request.Tags.Count())
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var course = request.Adapt<Course>();

        var uploadedPath = await _fileService.UploadImageAsync(uploadImage.Image, _folderName, cancellationToken);

        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.ImageUrl = uploadedPath;
        course.Tags = tags;

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(course.Adapt<CourseResponse>());
    }

    public async Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request, UploadImageRequest? uploadImage, string userId, CancellationToken cancellationToken = default)
    {
        int[] numbers = _hashids.Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseResponse>(CourseErrors.CourseTagNotFound);

        request.Adapt(course);

        if (uploadImage?.Image is not null)
        {
            if (!string.IsNullOrWhiteSpace(course.ImageUrl))
                _fileService.Delete(course.ImageUrl);

            var newImagePath = await _fileService.UploadImageAsync(uploadImage.Image, _folderName, cancellationToken);

            course.ImageUrl = newImagePath;
        }

        course.Tags = tags;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(GetBaseUrl(), course.ImageUrl) };

        return Result.Success(response);
    }




    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;

        return $"{request.Scheme}://{request.Host}";
    }
}