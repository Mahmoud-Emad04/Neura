namespace Neura.Core.Contracts.Review;

public record ReviewRequest(
    int Rating,
    string? Comment
);
