using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Neura.Core.Abstractions.Consts;
using System.Security.Claims;

namespace Neura.Services.Authentication.Filters;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _http;
    private readonly ApplicationDbContext _db;
    private readonly Hashids _hashids = new("Course", 8);

    public PermissionAuthorizationHandler(
        IHttpContextAccessor http,
        ApplicationDbContext db)
    {
        _http = http;
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasGlobal = context.User.Claims.Any(
            c => c.Type == Permissions.Type && c.Value == requirement.Permission);

        if (hasGlobal)
        {
            context.Succeed(requirement);
            return;
        }

        if (!requirement.Permission.StartsWith("courses:"))
            return;

        var httpContext = _http.HttpContext;
        if (httpContext == null)
            return;

        var rawPerm = requirement.Permission.Split(':')[1];

        var values = httpContext.GetRouteData()?.Values;


        if (values is null || !values.TryGetValue("courseId", out var cid))
            return;

        var encodedId = cid?.ToString();
        if (encodedId is null)
            return;

        var numbers = _hashids.Decode(encodedId);
        if (numbers.Length == 0)
            return;

        var courseId = numbers[0];

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return;

        var role = await _db.CourseUsers
            .Where(cu => cu.CourseId == courseId && cu.UserId == userId)
            .Select(cu => cu.Role)
            .FirstOrDefaultAsync();

        if (role is null)
            return;

        var perms = CoursePermissions.RolePermissions[role];

        if (perms.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
