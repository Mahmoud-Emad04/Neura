namespace GraduationProject.Core.Contracts.Authentication;

public record AuthResponse(
    string Id,
    string Username,
    string? Email,
    string FirstName,
    string LastName,
    string Token,
    int Expiresin,
    string RefreshToken,
    DateTime RefreshTokenExpiration);