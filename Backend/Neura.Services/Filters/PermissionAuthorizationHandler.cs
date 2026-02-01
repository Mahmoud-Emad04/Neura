using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Neura.Core.Abstractions.Consts;

namespace Neura.Services.Filters;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly Hashids _hashids = new("Course", 8);
    private readonly IHttpContextAccessor _http;

    public PermissionAuthorizationHandler(
        IHttpContextAccessor http,
        ApplicationDbContext context)
    {
        _http = http;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasGlobal = context.User.Claims.Any(c => c.Type == Permissions.Type && c.Value == requirement.Permission);

        if (hasGlobal)
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.Permission.StartsWith("Course:"))
        {
            var httpContext = _http.HttpContext;

            if (httpContext == null)
                return;

            var split = requirement.Permission.Split(':');

            var rawPerm = $"{split[1]}:{split[2]}"; //course:update

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

            if (string.IsNullOrEmpty(userId))
                return;

            var user = await _context.CourseUsers
                .Where(cu => cu.CourseId == courseId && cu.UserId == userId)
                .FirstOrDefaultAsync();

            if (user is null)
                return;

            var permMask = CoursePermissionMap.Map[rawPerm];

            if (CoursePermissionMap.Map.TryGetValue(rawPerm, out var value) && (value & permMask) != 0)
                context.Succeed(requirement);
        }
    }
}