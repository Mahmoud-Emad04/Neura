using Microsoft.AspNetCore.Authorization;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authorization.Requirements;
using System.Linq.Dynamic.Core;

namespace Neura.Services.Authorization.Handlers;

public class LessonPermissionAuthorizationHandler : AuthorizationHandler<LessonPermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    public LessonPermissionAuthorizationHandler(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LessonPermissionRequirement requirement)
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
        var lessonId = GetLessonIdFromRoute();
        var courseId = _context.Lessons.Where(l => l.Id == lessonId).Select(l => (int?)l.Section.CourseId).FirstOrDefault();

        if (courseId is null) return;

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
    private int? GetLessonIdFromRoute()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null) return null;

        // Try to get from route values - check common parameter names
        string? rawLessonId = null;

        if (httpContext.Request.RouteValues.TryGetValue("id", out var lessonIdValue))
            rawLessonId = lessonIdValue?.ToString();
        else if (httpContext.Request.RouteValues.TryGetValue("lessonId", out var idValue)) rawLessonId = idValue?.ToString();

        if (string.IsNullOrEmpty(rawLessonId))
            // Try to get from query string
            if (httpContext.Request.Query.TryGetValue("id", out var queryCourseId))
                rawLessonId = queryCourseId.FirstOrDefault();

        if (string.IsNullOrEmpty(rawLessonId)) return null;

        // Try to parse as integer first
        if (int.TryParse(rawLessonId, out var intLessonId)) return intLessonId;

        return null;
    }
}
