using Microsoft.AspNetCore.Authorization;
using Neura.Core.Abstractions.Consts;
using Neura.Core.Authorization.Requirements;
using System.Linq.Dynamic.Core;

namespace Neura.Services.Authorization.Handlers;

public class SectionPermissionAuthorizationHandler : AuthorizationHandler<SectionPermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    public SectionPermissionAuthorizationHandler(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SectionPermissionRequirement requirement)
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
        var SectionId = GetSectionIdFromRoute();
        var courseId = _context.Sections.IgnoreQueryFilters().Where(s => s.Id == SectionId).Select(l => (int?)l.CourseId).FirstOrDefault();

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
    private int? GetSectionIdFromRoute()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null) return null;

        // Try to get from route values - check common parameter names
        string? rawSectionId = null;

        if (httpContext.Request.RouteValues.TryGetValue("id", out var sectionIdValue))
            rawSectionId = sectionIdValue?.ToString();
        else if (httpContext.Request.RouteValues.TryGetValue("sectionId", out var idValue)) rawSectionId = idValue?.ToString();

        if (string.IsNullOrEmpty(rawSectionId))
            // Try to get from query string
            if (httpContext.Request.Query.TryGetValue("id", out var queryCourseId))
                rawSectionId = queryCourseId.FirstOrDefault();

        if (string.IsNullOrEmpty(rawSectionId)) return null;

        // Try to parse as integer first
        if (int.TryParse(rawSectionId, out var intSectionId)) return intSectionId;

        return null;
    }
}
