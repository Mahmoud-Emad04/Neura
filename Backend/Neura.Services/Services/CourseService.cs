using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Files;
using Neura.Core.FilesConsts;

namespace Neura.Services.Services;

public class CourseService(ApplicationDbContext context,
                            IFileService fileService,
                            IHttpContextAccessor httpContextAccessor,
                            RoleManager<ApplicationRole> roleManager,
                            UserManager<ApplicationUser> userManager,
                            ILogger<CourseService> logger) : ICourseService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IFileService _fileService = fileService;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<CourseService> _logger = logger;
    private readonly Hashids _hashids = new("Course", 8);
    public async Task<Result<IEnumerable<CourseResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var courses = await _context.Courses
            .Include(c => c.Topics)
            .Include(c => c.Tags)
            .AsNoTracking()
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
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        if (course is null)
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(GetBaseUrl(), course.ImageUrl) };

        _logger.LogInformation($"{GetBaseUrl()}");
        _logger.LogInformation($"{Path.Combine(GetBaseUrl(), course.ImageUrl)}");

        return Result.Success(response);
    }

    public async Task<Result<CourseResponse>> CreateAsync(CourseRequest request, string userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await _context.Tags
                        .Where(t => request.Tags.Contains(t.Id))
                        .ToListAsync(cancellationToken);

        if (tags.Count() != request.Tags.Count())
            return Result.Failure<CourseResponse>(CourseErrors.CourseNotFound);

        var course = request.Adapt<Course>();

        course.CreatedById = userId;
        course.CreatedOn = DateTime.UtcNow;
        course.Tags = tags;
        course.ImageUrl = DefaultCourseImagePath();

        _logger.LogInformation("Image with Title {title} & Url {imageurl} Created", course.Title, course.ImageUrl);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        await CreateDynamicRoles(course);

        var ownerUser = await userManager.FindByIdAsync(userId);

        var ownerRoleName = $"{DefaultRoles.CourseOwner}:{course.Id}";

        await userManager.AddToRoleAsync(ownerUser!, ownerRoleName);

        _context.CourseUsers.Add(new CourseUser
        {
            CourseId = course.Id,
            UserId = ownerUser!.Id,
            Role = DefaultRoles.CourseOwner
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(course.Adapt<CourseResponse>());
    }
    public async Task<Result> UpdateImageAsync(string keyId, UploadImageRequest uploadImage, string userId, CancellationToken cancellationToken = default)
    {
        int[] numbers = _hashids.Decode(keyId);

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

        course.Tags = tags;
        course.UpdatedOn = DateTime.UtcNow;
        course.UpdatedById = userId;

        await _context.SaveChangesAsync(cancellationToken);

        var response = course.Adapt<CourseResponse>() with { ImageUrl = Path.Combine(GetBaseUrl(), course.ImageUrl) };

        return Result.Success(response);
    }

    private async Task CreateDynamicRoles(Course course)
    {
        var courseRoles = new List<string>
                {
                    DefaultRoles.CourseOwner,
                    DefaultRoles.CoInstructor,
                    DefaultRoles.TeachingAssistant,
                    DefaultRoles.Student
                };

        foreach (var role in courseRoles)
        {
            var roleName = $"{role}:{course.Id}";
            if (await roleManager.FindByNameAsync(roleName) == null)
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });

                // Copy claims from template
                var templateRole = await roleManager.FindByIdAsync(
                    role switch
                    {
                        DefaultRoles.CourseOwner => DefaultRoles.CourseOwnerRoleId,
                        DefaultRoles.CoInstructor => DefaultRoles.CoInstructorRoleId,
                        DefaultRoles.TeachingAssistant => DefaultRoles.TeachingAssistantRoleId,
                        DefaultRoles.Student => DefaultRoles.StudentRoleId,
                        _ => throw new Exception("Unknown role")
                    });

                var claims = await roleManager.GetClaimsAsync(templateRole!);
                foreach (var claim in claims)
                {
                    await roleManager.AddClaimAsync(await roleManager.FindByNameAsync(roleName), claim);
                }
            }
        }

    }

    private string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;

        return $"{request.Scheme}://{request.Host}";
    }

    private string DefaultCourseImagePath()
    {
        return Path.Combine("Images", ImageConsts.Course, ImageConsts.DefaultCourseImage);
    }
}