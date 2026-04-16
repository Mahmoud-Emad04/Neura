using Microsoft.AspNetCore.Authorization;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authorization.Requirements;

namespace Neura.Services.Authorization.Handlers;

/// <summary>
///     Handles course-level permission authorization
///     Supports both integer IDs and hashed string IDs
/// </summary>
public class CoursePermissionAuthorizationHandler : AuthorizationHandler<CoursePermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;


    public CoursePermissionAuthorizationHandler(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CoursePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;

        var userId = _userManager.GetUserId(context.User);

        if (string.IsNullOrEmpty(userId)) return;

        // Check if user is Admin or SuperAdmin (bypass course permissions)
        if (context.User.IsInRole(DefaultRoles.SuperAdmin) ||
            context.User.IsInRole(DefaultRoles.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        // Get courseId from route
        var courseId = GetCourseIdFromRoute();

        if (courseId is null) return;

        // Get user's course membership
        var courseUser = await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);

        if (courseUser is null) return;

        // Check permission using bitwise operation
        if (CoursePermissionMasks.HasPermission(courseUser.PermissionMask, requirement.Permission))
            context.Succeed(requirement);
    }

    private int? GetCourseIdFromRoute()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null) return null;

        // Try to get from route values - check common parameter names
        string? rawCourseId = null;

        if (httpContext.Request.RouteValues.TryGetValue("courseId", out var courseIdValue))
            rawCourseId = courseIdValue?.ToString();
        else if (httpContext.Request.RouteValues.TryGetValue("id", out var idValue)) rawCourseId = idValue?.ToString();

        if (string.IsNullOrEmpty(rawCourseId))
            // Try to get from query string
            if (httpContext.Request.Query.TryGetValue("courseId", out var queryCourseId))
                rawCourseId = queryCourseId.FirstOrDefault();

        if (string.IsNullOrEmpty(rawCourseId)) return null;

        // Try to parse as integer first
        if (int.TryParse(rawCourseId, out var intCourseId)) return intCourseId;

        // Try to decode as hashed ID
        try
        {
            var hashids = new Hashids("f1nd1ngn3m0", 11);
            var numbers = hashids.Decode(rawCourseId);

            if (numbers.Length == 0) return null;
            var decodedId = numbers[0];
            return decodedId;
        }
        catch
        {
            return null;
        }
    }
}