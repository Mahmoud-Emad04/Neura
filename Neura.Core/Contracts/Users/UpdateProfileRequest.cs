namespace Neura.Core.Contracts.Users;

public record UpdateProfileRequest(
    string FirstName,
    string LastName
);