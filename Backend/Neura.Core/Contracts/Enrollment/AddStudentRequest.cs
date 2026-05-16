using System.ComponentModel.DataAnnotations;

namespace Neura.Core.Contracts.Enrollment;

public class AddStudentRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}