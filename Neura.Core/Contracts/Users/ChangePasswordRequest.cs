namespace Neura.Core.Contracts.Users;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);