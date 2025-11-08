namespace Neura.Core.Contracts.Authentication;

public record RegisterRequest(
    string UserName,
    string Email,
    string? DiscordHandle,
    string FirstName,
    string LastName,
    string Password
);