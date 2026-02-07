using Neura.Core.Abstractions;

namespace Neura.Core.Errors;

public static class UserErrors
{
    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "Invalid email or password. Please try again.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error UserNotFound = new(
        "User.NotFound",
        "The requested user could not be found.",
        StatusCodes.Status404NotFound);

    // 3. TOKEN ERRORS
    public static readonly Error InvalidJwtToken = new(
        "User.InvalidJwtToken",
        "Your session is invalid or has expired. Please log in again.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InValidRefreshToken = new(
        "User.InvalidRefreshToken",
        "Your session has expired. Please log in again.",
        StatusCodes.Status401Unauthorized);

    // 4. REGISTRATION ERRORS
    public static readonly Error DuplicatedEmail = new(
        "User.DuplicateEmail",
        "This email is already in use. Please sign in or use a different email.",
        StatusCodes.Status409Conflict);

    public static readonly Error DuplicatedUserName = new(
        "User.DuplicateUserName",
        "This username is already taken. Please choose a different one.", // Fixed typo "his" -> "This"
        StatusCodes.Status409Conflict);

    // 5. EMAIL CONFIRMATION ERRORS
    public static readonly Error EmailNotConfirmed = new(
        "User.EmailNotConfirmed",
        "Please confirm your email address before logging in. Check your inbox for the activation link.",
        StatusCodes.Status403Forbidden);

    public static readonly Error InvalidCode = new(
        "User.InvalidCode",
        "This confirmation link is invalid or has expired. Please request a new one.",
        StatusCodes.Status400BadRequest); // Changed 404 -> 400 (Bad Request is more appropriate for expired logic)

    public static readonly Error DuplicatedConfirmation = new(
        "User.DuplicateConfirmation",
        "Your email is already confirmed. You can log in now.",
        StatusCodes.Status409Conflict);
}