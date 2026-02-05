namespace Neura.Core.Contracts;

public record ReviewRequest(
    int Rating,
    string? Comment
);