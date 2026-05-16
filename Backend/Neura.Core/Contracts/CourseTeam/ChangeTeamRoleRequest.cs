using System.ComponentModel.DataAnnotations;
using Neura.Core.Enums;

namespace Neura.Core.Contracts.CourseTeam;

public class ChangeTeamRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    public CourseRoleType NewRole { get; set; }
}