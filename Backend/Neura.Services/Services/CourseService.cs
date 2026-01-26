using System.Security.Claims;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Files;
using Neura.Core.FilesConsts;
using Neura.Core.Contracts;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class CourseService(ApplicationDbContext context,
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
    private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<CourseService> _logger = logger;

    string BaseUrl() => _helpers.GetBaseUrl();
    int[] Decode(string key) => _helpers.DecodeHash(key);

    public async Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _context.Courses
            .Include(c => c.Tags)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var course in courses)
            course.ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl);

        var response = courses.Adapt<IEnumerable<CourseResponse>>();

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> GetByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

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
    public async Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage, string userId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

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
    public async Task<Result<CourseResponse>> UpdateAsync(string keyId, CourseUpdateRequest request, string userId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

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

    public async Task<Result> DeleteAsync(string keyId, string userId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        // Soft-delete: mark as deleted instead of removing
        course.IsDeleted = true;
        course.DeletedOn = DateTime.UtcNow;
        course.DeletedById = userId;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        // If you want to remove stored image on delete, uncomment below. For soft-delete we keep the asset.
        // if (course.ImageUrl != DefaultCourseImagePath())
        //     _fileService.Delete(course.ImageUrl);

        //_context.Courses.Remove(course);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PagedResult<CourseResponse>>> GetPagedAsync(int page = 1, int pageSize = 10, int? tagId = null, CancellationToken cancellationToken = default)
    {
        if (page <= 0 || pageSize <= 0)
            return Result.Failure<PagedResult<CourseResponse>>(CourseErrors.CourseInvalidData);

        IQueryable<Course> query = _context.Courses
            .Include(c => c.Tags)
            .Include(c => c.Topics)
            .AsNoTracking();

        if (tagId.HasValue)
        {
            query = query.Where(c => c.Tags.Any(t => t.Id == tagId.Value));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var course in items)
            course.ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl);

        var responseItems = items.Adapt<IEnumerable<CourseResponse>>();

        var paged = new PagedResult<CourseResponse>(responseItems, page, pageSize, total, (int)Math.Ceiling((double)total / pageSize));

        return Result.Success(paged);
    }

    public async Task<Result<PagedResult<CourseResponse>>> GetDeletedAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        if (page <= 0 || pageSize <= 0)
            return Result.Failure<PagedResult<CourseResponse>>(CourseErrors.CourseInvalidData);

        var query = _context.Courses
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted)
            .Include(c => c.Tags)
            .Include(c => c.Topics)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.DeletedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var course in items)
            course.ImageUrl = Path.Combine(BaseUrl(), course.ImageUrl);

        var responseItems = items.Adapt<IEnumerable<CourseResponse>>();

        var paged = new PagedResult<CourseResponse>(responseItems, page, pageSize, total, (int)Math.Ceiling((double)total / pageSize));

        return Result.Success(paged);
    }

    public async Task<Result> RestoreAsync(string keyId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure(CourseErrors.CourseNotFound);

        course.IsDeleted = false;
        course.DeletedOn = null;
        course.DeletedById = null;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> PurgeAsync(string keyId, CancellationToken cancellationToken = default)
    {
        int[] numbers = Decode(keyId);

        if (numbers.Length == 0)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        int courseId = numbers[0];

        var course = await _context.Courses
            .IgnoreQueryFilters()
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        // delete stored image if not default
        if (course.ImageUrl != DefaultCourseImagePath())
            _fileService.Delete(course.ImageUrl);

        _context.Courses.Remove(course);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }
}