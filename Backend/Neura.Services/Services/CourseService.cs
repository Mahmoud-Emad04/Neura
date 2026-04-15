using Neura.Core.Abstractions.Consts;
using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Courses;
using Neura.Core.Contracts.Files;
using Neura.Core.Enums;
using Neura.Core.FilesConsts;
using Neura.Core.Specifications.Courses;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class CourseService(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers,
    UserManager<ApplicationUser> userManager,
    ILogger<CourseService> logger) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<CourseService> _logger = logger;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<CourseSummaryResponse>>> GetAllAsync(
        RequestFilters filters,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var spec = new CourseFilterSpecification(filters);

        var query = SpecificationEvaluator.GetQuery(_context.Courses.AsNoTracking(), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = BaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            cancellationToken
        );
        var studentMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.Student];

        foreach (var course in paginatedCourses.Items)
        {
            TryDecodeCourseId(course.KeyId, out var courseId);
            course.NumberOfStudents =
                await _context.CourseUsers.CountAsync(c => c.CourseId == courseId && !c.IsDeleted, cancellationToken);
        }

        if (userId is not null)
        {
            var bookmarkedCourseIds = await _context.CourseBookmarks
                .Where(b => b.UserId == userId && !b.IsDeleted)
                .Select(b => b.CourseId)
                .ToListAsync(cancellationToken);

            foreach (var course in paginatedCourses.Items)
                if (TryDecodeCourseId(course.KeyId, out var id))
                {
                    course.IsBookmarked = bookmarkedCourseIds.Contains(id);
                    course.IsEnrolled =
                        await _context.CourseUsers.AnyAsync(cu => cu.CourseId == id && cu.UserId == userId,
                            cancellationToken);
                }
        }

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<CourseResponse>> GetContentByIdAsync(string keyId, string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var totalCourseMinutes = course.Sections.SelectMany(s => s.Lessons)
            .Sum(l => l.Duration.TotalMinutes);

        var response = course.Adapt<CourseResponse>() with
        {
            Hours = (int)Math.Round(totalCourseMinutes / 60.0)
        };

        return Result.Success(response);
    }

    public async Task<Result<CourseMetadataResponse>> GetCourseMetadataAsync(string keyId, string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .Include(c => c.Tags)
            .Include(c => c.Prerequisites)
            .Include(c => c.LearningOutcomes)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var studentRoleMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.Student];

        var response = course.Adapt<CourseMetadataResponse>() with
        {
            ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl),

            NumberOfStudents = await _context.CourseUsers
                .CountAsync(cu => cu.CourseId == courseId && cu.PermissionsMask == studentRoleMask && !cu.IsDeleted,
                    cancellationToken),
            IsEnrolled = userId is not null &&
                         await _context.CourseUsers.AnyAsync(cu => cu.CourseId == courseId && cu.UserId == userId,
                             cancellationToken),

            IsBookmarked = userId is not null &&
                           await _context.CourseBookmarks.AnyAsync(
                               cb => cb.CourseId == courseId && cb.UserId == userId && !cb.IsDeleted,
                               cancellationToken),

            IsOwner = userId is not null && userId == course.CreatedById
        };

        return Result.Success(response);
    }

    public async Task<Result<CourseMetadataResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseTagNotFound);

        var ownerUser = await _userManager.FindByIdAsync(userId);

        var course = request.Adapt<Course>();

        course.DisplayInstructorName = $"{ownerUser!.FirstName} {ownerUser.LastName}";
        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;

        if (request.Image is not null)
            course.ImageUrl = await _fileService.UploadImageAsync(request.Image, ImageConsts.Course, cancellationToken);
        else
            course.ImageUrl = DefaultCourseImagePath();

        _logger.LogInformation("Image with Title {title} & Url {imageurl} Created", course.Title, course.ImageUrl);

        _context.Courses.Add(course);

        await _context.SaveChangesAsync(cancellationToken);

        CourseUser courseUser = new()
        {
            CourseId = course.Id,
            UserId = userId,
            PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner]
        };

        await _context.CourseUsers.AddAsync(courseUser, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(course.Adapt<CourseMetadataResponse>());
    }

    public async Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage, string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (course.ImageUrl != DefaultCourseImagePath())
            _fileService.Delete(course.ImageUrl);

        course.ImageUrl = await _fileService.UploadImageAsync(uploadImage.Image, ImageConsts.Course, cancellationToken);
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CourseMetadataResponse>> UpdateAsync(string keyId, CourseUpdateRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseNotFound);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseMetadataResponse>(CourseErrors.CourseTagNotFound);

        _context.CourseLearningOutcomes.RemoveRange(course.LearningOutcomes);
        course.IsPubliclyVisible = request.IsPubliclyVisible;

        await _context.SaveChangesAsync(cancellationToken);

        var response = course.Adapt<CourseMetadataResponse>() with { };

        return Result.Success(response);
    }

    public async Task<Result> EnrollAsync(string keyId, string userId, CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var studentMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.Student];

        var courseUser = await _context.CourseUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, cancellationToken);

        if (courseUser is null)
        {
            courseUser = new CourseUser
            {
                CourseId = courseId,
                UserId = userId,
                PermissionsMask = studentMask,
                IsDeleted = false
            };

            await _context.CourseUsers.AddAsync(courseUser, cancellationToken);
        }
        else if (courseUser.IsDeleted)
        {
            courseUser.IsDeleted = false;
            courseUser.PermissionsMask = studentMask;
        }
        else
        {
            // Optional: Ensure they have at least Student access
            courseUser.PermissionsMask |= studentMask;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnenrollAsync(string keyId, string userId, CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        var enrollment = await _context.CourseUsers
            .SingleOrDefaultAsync(cu => cu.CourseId == courseId && cu.UserId == userId, cancellationToken);

        if (enrollment is null || enrollment.IsDeleted)
            return Result.Failure(CourseErrors.UserNotEnrolled);

        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];

        if ((enrollment.PermissionsMask & ownerMask) == ownerMask)
            return Result.Failure(CourseErrors.OwnerCannotUnenroll);

        enrollment.IsDeleted = true;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<CourseMetadataResponse>>> GetEnrolledCoursesAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var ownerMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner];

        var courses = await _context.CourseUsers
            .Where(cu => cu.UserId == userId && !cu.IsDeleted && (ownerMask & cu.PermissionsMask) != ownerMask)
            .Include(cu => cu.Course)
            .Select(cu => cu.Course)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
            course.ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl);

        var response = courses.Adapt<IEnumerable<CourseMetadataResponse>>();

        return Result.Success(response);
    }

    public async Task<Result> ToggleBookmarkAsync(string keyId, string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure(CourseErrors.CourseNotFound);

        if (await _context.CourseBookmarks.FirstOrDefaultAsync(cb => cb.CourseId == courseId && cb.UserId == userId,
                cancellationToken) is not { } bookmark)
            await _context.CourseBookmarks.AddAsync(
                new CourseBookmark
                    { CourseId = courseId, UserId = userId, IsDeleted = false, CreatedOn = DateTime.UtcNow },
                cancellationToken);
        else
            bookmark.IsDeleted = !bookmark.IsDeleted;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<PaginatedList<CourseSummaryResponse>>> GetBookmarkedAsync
        (string userId, RequestFilters filters, CancellationToken cancellationToken = default)
    {
        var spec = new BookmarkedCoursesFilterSpecification(userId, filters);

        var query = SpecificationEvaluator.GetQuery(_context.CourseBookmarks.AsNoTracking(), spec);

        var projectedQuery = query.ProjectToType<CourseSummaryResponse>();

        var baseUrl = BaseUrl();

        var paginatedCourses = await PaginatedList<CourseSummaryResponse>.CreateAsync(
            projectedQuery,
            filters.PageNumber,
            filters.PageSize,
            c => c.ImageUrl = $"{baseUrl}/{c.ImageUrl}",
            cancellationToken
        );

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<CourseStatusResponse>> GetCourseStatusAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusResponse>(CourseErrors.CourseNotFound);

        ActivationRequirements? requirements = null;

        if (course.Status == CourseStatus.Pending)
            requirements = await GetActivationRequirementsAsync(courseId, cancellationToken);

        var response = new CourseStatusResponse
        {
            KeyId = keyId,
            Status = course.Status,
            StatusName = course.Status.ToString(),
            IsEnrollmentOpen = course.IsEnrollmentOpen,
            IsAccessibleToStudents = course.IsAccessibleToStudents,
            IsPubliclyVisible = course.IsPubliclyVisible,
            CanActivate = course.Status == CourseStatus.Pending,
            CanComplete = course.Status == CourseStatus.Active,
            CanReactivate = course.Status == CourseStatus.Completed,
            CanUnpublish = course.Status == CourseStatus.Active,
            Requirements = requirements
        };

        return Result.Success(response);
    }

    public async Task<Result<CourseStatusUpdateResponse>> ActivateCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var hasPublishedContent = await _context.Lessons
            .AsNoTracking()
            .AnyAsync(l =>
                    l.Section.CourseId == courseId &&
                    l.IsPublished &&
                    !l.IsDeleted &&
                    !l.Section.IsDeleted,
                cancellationToken);

        if (!hasPublishedContent)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseHasNoPublishedContent);

        var previousStatus = course.Status;

        var result = course.Activate();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} activated by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course activated successfully. Students can now enroll.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> CompleteCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Complete();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} completed by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message =
                "Course marked as completed. Enrolled students can still access content, but no new enrollments allowed.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> ReactivateCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Reactivate();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} reactivated by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course reactivated successfully. New students can now enroll.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    public async Task<Result<CourseStatusUpdateResponse>> UnpublishCourseAsync(
        string keyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseStatusUpdateResponse>(CourseErrors.CourseNotFound);

        var previousStatus = course.Status;

        var result = course.Unpublish();
        if (result.IsFailure)
            return Result.Failure<CourseStatusUpdateResponse>(result.Error);

        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Course {CourseId} unpublished by user {UserId}. Status changed from {PreviousStatus} to {CurrentStatus}",
            courseId,
            userId,
            previousStatus,
            course.Status);

        return Result.Success(new CourseStatusUpdateResponse
        {
            KeyId = keyId,
            PreviousStatus = previousStatus,
            CurrentStatus = course.Status,
            Message = "Course unpublished. It is now hidden from public listings.",
            UpdatedAt = course.UpdatedOn.Value
        });
    }

    private bool TryDecodeCourseId(string keyId, out int courseId)
    {
        var numbers = _helpers.DecodeHash(keyId);
        if (numbers.Length == 0)
        {
            courseId = 0;
            return false;
        }

        courseId = numbers[0];
        return true;
    }

    private string BaseUrl()
    {
        return _helpers.GetBaseUrl();
    }

    private static string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }

    private async Task<ActivationRequirements> GetActivationRequirementsAsync(
        int courseId,
        CancellationToken cancellationToken)
    {
        var stats = await _context.Courses
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
            .SingleOrDefaultAsync(cancellationToken);

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
}