using System.ComponentModel.DataAnnotations;
using Neura.Core.Enums;

namespace Neura.Core.Contracts.CourseTeam;

public class InviteTeamMemberRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public CourseRoleType Role { get; set; }

    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters")]
    public string? CustomMessage { get; set; }
}