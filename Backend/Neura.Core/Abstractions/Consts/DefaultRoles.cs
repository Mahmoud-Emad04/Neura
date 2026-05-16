namespace Neura.Core.Abstractions.Consts;

public static class DefaultRoles
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string Admin = nameof(Admin);
    public const string Instructor = nameof(Instructor);
    public const string Member = nameof(Member);

    /// <summary>
    ///     All roles in order of privilege (highest first)
    /// </summary>
    public static readonly string[] All =
    [
        SuperAdmin,
        Admin,
        Instructor,
        Member
    ];
}