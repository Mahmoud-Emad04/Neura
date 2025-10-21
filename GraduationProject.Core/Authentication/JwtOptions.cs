using System.ComponentModel.DataAnnotations;

namespace GraduationProject.Core.Authentication;

public class JwtOptions
{
    public static string SectionName = "Jwt";
    [Required] public string Key { get; set; } = string.Empty!;

    [Required] public string Issuer { get; set; } = string.Empty!;

    [Required] public string Audience { get; set; } = string.Empty!;

    [Range(1, int.MaxValue, ErrorMessage = "Range Is Not Valid")]
    public int ExpiryMinutes { get; set; }
}