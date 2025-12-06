using Microsoft.AspNetCore.Identity;

namespace Neura.Core.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string? DiscordHandle { get; set; } = string.Empty;
    public List<RefreshTokens> RefreshTokens { get; set; } = [];
    public ICollection<CourseUser> CourseUsers { get; set; } = [];
}