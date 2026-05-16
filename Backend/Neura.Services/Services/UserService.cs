using Neura.Core.Abstractions.Consts;
using Neura.Core.Contracts.Instructor;
using Neura.Core.Contracts.Users;
using Neura.Services.Helpers;

namespace Neura.Services.Services;

public class UserService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    IServiceHelpers helpers,
    ILogger<UserService> logger) : IUserService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IServiceHelpers _helpers = helpers;
    private readonly ILogger<UserService> _logger = logger;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId)
    {
        var user = await _userManager.Users.Where(u => u.Id == userId)
            .ProjectToType<UserProfileResponse>()
            .SingleAsync();

        return Result.Success(user);
    }

    public async Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        await _userManager.Users
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(x => x.FirstName, request.FirstName)
                    .SetProperty(x => x.LastName, request.LastName)
            );

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        var result = await _userManager.ChangePasswordAsync(user!, request.CurrentPassword, request.NewPassword);

        if (result.Succeeded)
            return Result.Success();

        var error = result.Errors.First();

        return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
    }

    public async Task SendMail()
    {
        await Task.Delay(7000);
        _logger.LogInformation("HangFire");
    }

    public async Task<Result<InstructorSummaryResponse>> GetInstructorByCourseId(string keyId,
        CancellationToken cancellationToken = default)
    {
        if (!TryDecodeCourseId(keyId, out var courseId))
            return Result.Failure<InstructorSummaryResponse>(CourseErrors.CourseNotFound);

        var course = await _context.Courses.FindAsync(courseId, cancellationToken);
        if (course is null)
            return Result.Failure<InstructorSummaryResponse>(CourseErrors.CourseNotFound);

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Id == course.CreatedById, cancellationToken);

        if (user is null)
            return Result.Failure<InstructorSummaryResponse>(UserErrors.UserNotFound);

        var instructorCourseIds = await _context.Courses
            .Where(c => c.CreatedById == course.CreatedById)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        instructorCourseIds ??= [];

        var (globalStudentCount, globalRating, globalRatingDataCount) =
            await GetStudentsAndRating(instructorCourseIds, cancellationToken);
        string baseUrl = BaseUrl();
        return Result.Success(user.Adapt<InstructorSummaryResponse>() with
        {
            Name = $"{user.FirstName} {user.LastName}",
            TotalStudents = globalStudentCount,
            TotalReviews = globalRatingDataCount,
            ImageUrl = string.IsNullOrEmpty(user.ImageUrl) ? null : $"{baseUrl}/{user.ImageUrl}",
            Rating = globalRating,
            TotalCourses = instructorCourseIds.Count
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

    private async Task<(int globalStudentCount, double globalRating, int globalRatingDataCount)> GetStudentsAndRating(
        List<int> instructorCourseIds, CancellationToken cancellationToken)
    {
        var studentRoleMask = CoursePermissionMasks.Student;

        var globalStudentCount = await _context.CourseUsers
            .Where(cu => instructorCourseIds.Contains(cu.CourseId) && cu.PermissionMask == studentRoleMask)
            .Select(cu => cu.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var globalRatingData = await _context.Reviews
            .Where(r => instructorCourseIds.Contains(r.CourseId))
            .Select(r => (double?)r.Rating)
            .ToListAsync(cancellationToken);

        var globalRating = globalRatingData.Count > 0 ? globalRatingData.Average() ?? 0 : 0;

        return (globalStudentCount, globalRating, globalRatingData.Count);
    }
    private string BaseUrl()
    {
        return _helpers.GetBaseUrl();
    }

}