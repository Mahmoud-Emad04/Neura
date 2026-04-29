using Microsoft.AspNetCore.Authorization;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authorization.Requirements;
using System.Linq.Dynamic.Core;

namespace Neura.Services.Authorization.Handlers;

public class ExamPermissionAuthorizationHandler : AuthorizationHandler<ExamPermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    public ExamPermissionAuthorizationHandler(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ExamPermissionRequirement requirement)
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
        var examId = GetExamIdFromRoute();
        var courseId = _context.Exams.Where(e => e.Id == examId).Select(e => (int?)e.Lesson.Section.CourseId).FirstOrDefault();

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
    private int? GetExamIdFromRoute()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null) return null;

        // Try to get from route values - check common parameter names
        string? rawExamId = null;

        if (httpContext.Request.RouteValues.TryGetValue("id", out var examIdValue))
            rawExamId = examIdValue?.ToString();
        else if (httpContext.Request.RouteValues.TryGetValue("examId", out var idValue)) rawExamId = idValue?.ToString();

        if (string.IsNullOrEmpty(rawExamId))
            // Try to get from query string
            if (httpContext.Request.Query.TryGetValue("id", out var queryCourseId))
                rawExamId = queryCourseId.FirstOrDefault();

        if (string.IsNullOrEmpty(rawExamId)) return null;

        // Try to parse as integer first
        if (int.TryParse(rawExamId, out var intSectionId)) return intSectionId;

        return null;
    }
}
