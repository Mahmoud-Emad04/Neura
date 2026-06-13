namespace Neura.Core.Contracts.Community;

public sealed record UnreadNotificationDto(
    string CourseId,
    int ChannelId,
    string ChannelName
);