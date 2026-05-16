using Neura.Core.Enums;

namespace Neura.Core.Entities;

/// <summary>
///     Reference table for predefined course roles
///     These are seeded and should not be modified by users
/// </summary>
public class CourseRole
{
    public int Id { get; set; }

    /// <summary>
    ///     Role name (Student, Assistant, CoInstructor, CourseOwner)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Bitwise permission mask for this role
    /// </summary>
    public int PermissionMask { get; set; }

    /// <summary>
    ///     Hierarchy level (1=lowest, 4=highest)
    ///     Higher level inherits all lower level permissions
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///     Human-readable description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     System roles cannot be deleted
    /// </summary>
    public bool IsSystem { get; set; } = true;

    /// <summary>
    ///     Navigation: Users with this role
    /// </summary>
    public ICollection<CourseUser> CourseUsers { get; set; } = [];

    /// <summary>
    ///     Get the enum type for this role
    /// </summary>
    public CourseRoleType RoleType => (CourseRoleType)Level;
}