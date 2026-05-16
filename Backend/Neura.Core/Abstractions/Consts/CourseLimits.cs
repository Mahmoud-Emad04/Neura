namespace Neura.Core.Abstractions.Consts;

/// <summary>
///     Course-related limits and constraints
/// </summary>
public static class CourseLimits
{
    /// <summary>
    ///     Maximum team members per course (excluding owner)
    /// </summary>
    public const int MaxTeamMembers = 10;

    /// <summary>
    ///     Days until invitation expires
    /// </summary>
    public const int InvitationExpiryDays = 7;

    /// <summary>
    ///     Days before rejected applicant can reapply
    /// </summary>
    public const int ReapplyWaitDays = 30;
}