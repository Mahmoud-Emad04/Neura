using System.ComponentModel.DataAnnotations;

namespace Neura.Core.InstructorApplication;

public class UpdateApplicationRequest
{
    [Required(ErrorMessage = "Bio is required")]
    [StringLength(2000, MinimumLength = 50, ErrorMessage = "Bio must be between 50 and 2000 characters")]
    public string Bio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Experience is required")]
    [StringLength(2000, MinimumLength = 50, ErrorMessage = "Experience must be between 50 and 2000 characters")]
    public string Experience { get; set; } = string.Empty;
}