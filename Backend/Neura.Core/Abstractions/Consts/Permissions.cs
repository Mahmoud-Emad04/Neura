namespace Neura.Core.Abstractions.Consts;

public static class Permissions
{
    public static string Type { get; } = "permissions";

    public const string GetCourses = "courses:read";
    public const string AddCourses = "courses:add";
    public const string UpdateCourses = "courses:update";
    public const string DeleteCourses = "courses:delete";

    public const string GetUsers = "users:read";
    public const string AddUsers = "users:add";
    public const string UpdateUsers = "users:update";

    public const string GetRoles = "roles:read";
    public const string AddRoles = "roles:add";
    public const string UpdateRoles = "roles:update";

    public const string Results = "results:read";

    public static List<string?> GetAll() =>
        typeof(Permissions).GetFields().Select(x => x.GetValue(x) as string).ToList();
}