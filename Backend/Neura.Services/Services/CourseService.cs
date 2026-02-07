using Neura.Core.Abstractions.Consts;
using Neura.Core.Abstractions.Specification;
using Neura.Core.Contracts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Files;
using Neura.Core.FilesConsts;
using Neura.Core.Specifications.Courses;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class CourseService(
    ApplicationDbContext context,
    IFileService fileService,
    IServiceHelpers helpers,
    IHttpContextAccessor httpContextAccessor,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<CourseService> logger) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<CourseService> _logger = logger;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
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

        if (userId is not null)
        {
            var userBoobmarks = _context.CourseBookmarks.Where(b => b.UserId == userId && !b.IsDeleted)
                .Select(b => b.CourseId)
                .ToList();

            foreach (var course in paginatedCourses.Items)
                course.IsBookmarked = userBoobmarks.Any(c => c == Decode(course.KeyId)[0]);
        }

        return Result.Success(paginatedCourses);
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        var numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Sections)
            .Include(c => c.Tags)
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl) };

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        if (request.Endin < request.Startin)
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count() != request.Tags.Count())
            return Result.Failure<CourseResponse>(CourseErrors.CourseTagNotFound);

        var ownerUser = await _userManager.FindByIdAsync(userId);

        var course = request.Adapt<Course>();

        course.InstructorName = $"{ownerUser!.FirstName} {ownerUser.LastName}";
        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;
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

        return Result.Success(course.Adapt<CourseResponse>());
    }

    public async Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage, string userId,
        CancellationToken cancellationToken = default)
    {
        var numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);

        var courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Sections)
            .Include(c => c.Tags)
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

    public async Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        // basic validation
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        if (request.Endin < request.Startin)
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count != request.Tags.Count)
            return Result.Failure<CourseResponse>(CourseErrors.CourseTagNotFound);

        request.Adapt(course);

        course.Tags = tags;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl) };

        return Result.Success(response);
    }

    public async Task<Result> EnrollAsync(string keyId, string userId, CancellationToken cancellationToken = default)
    {
        var numbers = Decode(keyId);
        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

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
        var numbers = Decode(keyId);
        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);

        var courseId = numbers[0];

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

    public async Task<Result<IEnumerable<CourseResponse>>> GetEnrolledCoursesAsync(string userId,
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

        var response = courses.Adapt<IEnumerable<CourseResponse>>();

        return Result.Success(response);
    }

    public async Task<Result> ToggleBookmarkAsync(string keyId, string userId, CancellationToken cancellationToken)
    {
        var numbers = Decode(keyId);
        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

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

    public async Task<Result> AddReviewAsync(string keyId, string userId, ReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return Result.Failure(ReviewErrors.InvalidRating);

        var numbers = Decode(keyId);
        if (numbers.Length == 0) return Result.Failure(CourseErrors.CourseNotFound);
        var courseId = numbers[0];

        var courseMeta = await _context.Courses
                                        .AsNoTracking()
                                        .Where(c => c.Id == courseId)
                                        .Select(c => new { c.CreatedById, c.IsDeleted })
                                        .FirstOrDefaultAsync(cancellationToken);

        if (courseMeta is null || courseMeta.IsDeleted)
            return Result.Failure(CourseErrors.CourseNotFound);

        if (courseMeta.CreatedById == userId)
            return Result.Failure(ReviewErrors.CannotReviewOwnCourse);

        var isEnrolled = await _context.CourseUsers
                                        .AsNoTracking()
                                        .AnyAsync(c => c.UserId == userId && c.CourseId == courseId && !c.IsDeleted, cancellationToken);

        if (!isEnrolled)
        {
            return Result.Failure(ReviewErrors.NotEnrolled);
        }

        var existingReview = await _context.Reviews
             .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId, cancellationToken);

        if (existingReview is not null)
        {
            existingReview.Rating = request.Rating;
            existingReview.Comment = request.Comment;
            existingReview.UpdatedOn = DateTime.UtcNow;
            _context.Reviews.Update(existingReview);
        }
        else
        {
            var review = new Review
            {
                CourseId = courseId,
                UserId = userId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedOn = DateTime.UtcNow
            };
            await _context.Reviews.AddAsync(review, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var stats = await _context.Reviews
            .Where(r => r.CourseId == courseId)
            .GroupBy(r => r.CourseId)
            .Select(g => new
            {
                Count = g.Count(),
                Average = g.Average(r => r.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stats is not null)
        {
            await _context.Courses
                .Where(c => c.Id == courseId)
                .ExecuteUpdateAsync(calls => calls
                    .SetProperty(c => c.TotalReviews, stats.Count)
                    .SetProperty(c => c.Rating, Math.Round(stats.Average, 1)),
                    cancellationToken);
        }

        return Result.Success();
    }
    public async Task<Result<PaginatedList<CourseSummaryResponse>>> GetBookmarkedAsync(string userId, RequestFilters filters, CancellationToken cancellationToken = default)
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

    private string BaseUrl()
    {
        return _helpers.GetBaseUrl();
    }

    private int[] Decode(string key)
    {
        return _helpers.DecodeHash(key);
    }

    private string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }

}