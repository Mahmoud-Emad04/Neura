namespace Neura.Core.Contracts.Community;

public sealed record UnreadNotificationDto(
    int CourseId,
    int ChannelId,
    string ChannelName
);