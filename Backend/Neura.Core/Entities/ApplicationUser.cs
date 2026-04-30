using Microsoft.AspNetCore.Identity;

namespace Neura.Core.Entities;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string? DiscordHandle { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    /// <summary>
    ///     User's biography (used for instructor profile)
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    ///     When user was approved as instructor (null if not instructor)
    /// </summary>
    public DateTime? InstructorApprovedOn { get; set; }

    /// <summary>
    ///     Computed: Is this user an approved instructor?
    /// </summary>
    public bool IsInstructor => InstructorApprovedOn.HasValue;

    // Navigation properties
    public List<RefreshTokens> RefreshTokens { get; set; } = [];
    public ICollection<CourseUser> CourseUsers { get; set; } = [];
    public ICollection<InstructorApplication> InstructorApplications { get; set; } = [];
    public ICollection<CourseInvitation> SentInvitations { get; set; } = [];
    public ICollection<CourseInvitation> ReceivedInvitations { get; set; } = [];
}