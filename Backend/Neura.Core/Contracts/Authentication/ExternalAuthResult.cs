public record ExternalAuthResult(
    bool IsSuccess,
    string? Token,
    string? RefreshToken,
    string? ErrorMessage
);