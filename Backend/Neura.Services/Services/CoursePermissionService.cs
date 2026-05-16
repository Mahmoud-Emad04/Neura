using Neura.Core.Abstractions.Consts;
using Neura.Core.Enums;

namespace Neura.Services.Services;

public class CoursePermissionService : ICoursePermissionService
{
    private readonly ApplicationDbContext _context;

    public CoursePermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasPermissionAsync(string userId, int courseId, CoursePermission permission)
    {
        var courseUser = await GetCourseUserAsync(userId, courseId);

        if (courseUser is null) return false;

        return CoursePermissionMasks.HasPermission(courseUser.PermissionMask, permission);
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, int courseId, params CoursePermission[] permissions)
    {
        var courseUser = await GetCourseUserAsync(userId, courseId);

        if (courseUser is null) return false;

        foreach (var permission in permissions)
            if (CoursePermissionMasks.HasPermission(courseUser.PermissionMask, permission))
                return true;

        return false;
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, int courseId, params CoursePermission[] permissions)
    {
        var courseUser = await GetCourseUserAsync(userId, courseId);

        if (courseUser is null) return false;

        foreach (var permission in permissions)
            if (!CoursePermissionMasks.HasPermission(courseUser.PermissionMask, permission))
                return false;

        return true;
    }

    public async Task<CourseUser?> GetCourseUserAsync(string userId, int courseId)
    {
        return await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .FirstOrDefaultAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);
    }

    public async Task<bool> IsEnrolledAsync(string userId, int courseId)
    {
        return await _context.CourseUsers
            .AsNoTracking()
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted);
    }

    public async Task<bool> IsTeamMemberAsync(string userId, int courseId)
    {
        return await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level >= (int)CourseRoleType.Assistant);
    }

    public async Task<bool> IsOwnerAsync(string userId, int courseId)
    {
        return await _context.CourseUsers
            .AsNoTracking()
            .Include(cu => cu.CourseRole)
            .AnyAsync(cu =>
                cu.CourseId == courseId &&
                cu.UserId == userId &&
                !cu.IsDeleted &&
                cu.CourseRole.Level == (int)CourseRoleType.CourseOwner);
    }
}