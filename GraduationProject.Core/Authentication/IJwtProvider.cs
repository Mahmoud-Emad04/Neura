using GraduationProject.Core.Entities;

namespace GraduationProject.Core.Authentication;

public interface IJwtProvider
{
    (string Token, int ExpiresIn) GenerateToken(ApplicationUser applicationUser);
    public string? ValidateToken(string token);
}