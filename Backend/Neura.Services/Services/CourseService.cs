using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.common;
using Neura.Core.Contracts.Files;
using Neura.Core.FilesConsts;
using Neura.Services.Helpers;
using System.Linq.Dynamic.Core;

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
    private readonly Hashids _hashids = new("Course", 8);
    private readonly IServiceHelpers _helpers = helpers;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<CourseService> _logger = logger;
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<CourseResponse>>> GetAllAsync(
        RequestFilters filters,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Courses
            .Where(c => !c.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(filters.SearchValue))
            query = query.Where(c =>
                c.Title.Contains(filters.SearchValue) ||
                c.Description.Contains(filters.SearchValue));

        if (!string.IsNullOrEmpty(filters.SortColumn))
            query = query.OrderBy($"{filters.SortColumn} {filters.SortDirection}");

        var source = query
            .Include(c => c.Tags)
            .ProjectToType<CourseResponse>();

        var baseUrl = BaseUrl();

        var paginatedCourses = await PaginatedList<CourseResponse>.CreateAsync(
            source,
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
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl) };

        _logger.LogInformation($"{BaseUrl()}");
        _logger.LogInformation($"{Path.Combine(BaseUrl(), course.ImageUrl)}");

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        // basic validation
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.InstructorName))
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        // validate dates
        if (request.Endin < request.Startin)
            return Result.Failure<CourseResponse>(CourseErrors.CourseInvalidData);

        var tags = await _context.Tags
            .Where(t => request.Tags.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tags.Count() != request.Tags.Count())
            return Result.Failure<CourseResponse>(CourseErrors.CourseTagNotFound);

        var course = request.Adapt<Course>();

        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;
        course.ImageUrl = DefaultCourseImagePath();

        _logger.LogInformation("Image with Title {title} & Url {imageurl} Created", course.Title, course.ImageUrl);

        _context.Courses.Add(course);

        await _context.SaveChangesAsync(cancellationToken);

        var ownerUser = await _userManager.FindByIdAsync(userId);

        //await _userManager.AddToRoleAsync(ownerUser!, DefaultRoles.CourseOwner);

        //var role = await _roleManager.FindByIdAsync(DefaultRoles.CourseOwnerRoleId);

        //var claims = await _roleManager.GetClaimsAsync(role!);

        //var permissions = claims
        //    .Where(c => c.Type == Permissions.Type)
        //    .Select(c => c.Value)
        //    .ToList();

        CourseUser courseUser = new()
        {
            CourseId = course.Id,
            UserId = userId,
            PermissionsMask = CourseRolePermissionMap.RolePermissionsMask[DefaultRoles.CourseOwner]
        };

        await _context.CourseUsers.AddAsync(courseUser, cancellationToken);

        //_logger.LogInformation("User {username} & mask {mask} Created", ownerUser!.UserName, permissionMask);

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
            .Include(c => c.Topics)
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

        if (await _context.CourseBookmarks.FirstOrDefaultAsync(cb => cb.CourseId == courseId && cb.UserId == userId, cancellationToken) is not { } bookmark)
        {
            await _context.CourseBookmarks.AddAsync(new CourseBookmark { CourseId = courseId, UserId = userId, IsDeleted = false, CreatedOn = DateTime.UtcNow }, cancellationToken);
        }
        else
            bookmark.IsDeleted = !bookmark.IsDeleted;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private string BaseUrl()
    {
        return _helpers.GetBaseUrl();
    }

    private int[] Decode(string key)
    {
        return _hashids.Decode(key);
    }

    private string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }
}