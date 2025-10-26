namespace Neura.Core.Contracts.Authentication;

public record LoginRequest(string UserNameOrEmail, string Password);
