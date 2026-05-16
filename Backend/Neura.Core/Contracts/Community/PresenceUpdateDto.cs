namespace Neura.Core.Contracts.Community;

public sealed record PresenceUpdateDto(
    string UserId,
    bool IsOnline
);