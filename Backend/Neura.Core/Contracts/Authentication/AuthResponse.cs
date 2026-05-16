namespace Neura.Core.Contracts.Authentication;

public record AuthResponse(
    string Id,
    string Username,
    string ImageUrl,
    string? DiscordHandle,
    string Email,
    string FirstName,
    string LastName,
    string Token,
    int Expiresin,
    string RefreshToken,
    DateTime RefreshTokenExpiration);