using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class UserErrors
{
    public static readonly Error UserNotFound = new("User.InvalidCredentials", "Invalid email/password",
        StatusCodes.Status404NotFound);

    public static readonly Error InvalidCodeOrUser = new("User.InvalidCredentials", "Invalid user/code",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken =
        new("User.InvalidJwtToken", "Invalid Jwt token", StatusCodes.Status401Unauthorized);

    public static readonly Error InValidRefreshToken = new("User.InvalidRefreshToken", "Invalid refresh token.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error DuplicatedEmail = new("User.DuplicateEmail",
        "This email address is already registered. Please use a different email or log in",
        StatusCodes.Status409Conflict);

    public static readonly Error EmailNotConfirmed = new("User.EmailNotConfirmed",
        "Your email address has not been confirmed yet. Please check your inbox for the confirmation link.",
        StatusCodes.Status403Forbidden);

    public static readonly Error InvalidCode = new("User.InvalidCode",
        "The confirmation code you entered is invalid or expired. Please try again or request a new code.",
        StatusCodes.Status404NotFound);

    public static readonly Error DuplicatedConfirmation = new("User.DuplicateConfirmation",
        "This email has already been confirmed. You can now log in to your account.",
        StatusCodes.Status409Conflict);

    public static readonly Error DuplicatedUserName = new("User.DuplicateUserName",
        "his username is already taken. Please choose a different one.",
        StatusCodes.Status409Conflict);
}