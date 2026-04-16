namespace Neura.Core.Enums;

/// <summary>
///     Global roles managed by ASP.NET Identity
///     Higher value = higher privilege level
/// </summary>
public enum GlobalRole
{
    /// <summary>
    ///     Unverified user (just registered)
    /// </summary>
    Guest = 0,

    /// <summary>
    ///     Verified registered user (email confirmed)
    /// </summary>
    Member = 1,

    /// <summary>
    ///     Approved content creator (can create courses)
    /// </summary>
    Instructor = 2,

    /// <summary>
    ///     Platform administrator (manage users, content)
    /// </summary>
    Admin = 3,

    /// <summary>
    ///     System owner (full control)
    /// </summary>
    SuperAdmin = 4
}