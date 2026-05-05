namespace Neura.Core.Contracts.Community;

public sealed record CourseMemberDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string RoleName,
    bool IsOnline,
    DateTime? LastSeenAt
);