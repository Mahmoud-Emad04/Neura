namespace GraduationProject.Core.Contracts.Authentication;

public record LoginRequest(string UserNameOrEmail, string Password);
