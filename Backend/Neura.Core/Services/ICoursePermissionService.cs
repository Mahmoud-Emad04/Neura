using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Core.Services;

/// <summary>
///     Service for checking course permissions programmatically
///     Use this when attribute-based authorization isn't suitable
/// </summary>
public interface ICoursePermissionService
{
    Task<bool> HasPermissionAsync(string userId, int courseId, CoursePermission permission);
    Task<bool> HasAnyPermissionAsync(string userId, int courseId, params CoursePermission[] permissions);
    Task<bool> HasAllPermissionsAsync(string userId, int courseId, params CoursePermission[] permissions);
    Task<CourseUser?> GetCourseUserAsync(string userId, int courseId);
    Task<bool> IsEnrolledAsync(string userId, int courseId);
    Task<bool> IsTeamMemberAsync(string userId, int courseId);
    Task<bool> IsOwnerAsync(string userId, int courseId);
}