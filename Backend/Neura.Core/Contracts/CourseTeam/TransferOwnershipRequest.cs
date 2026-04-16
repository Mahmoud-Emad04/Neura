using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.CourseTeam;

public class TransferOwnershipRequest
{
    [Required(ErrorMessage = "New owner ID is required")]
    public string NewOwnerId { get; set; } = string.Empty;
}