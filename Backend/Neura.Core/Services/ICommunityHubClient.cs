using Neura.Core.Contracts.Community;

namespace Neura.Core.Services;

public interface ICommunityHubClient
{
    Task PresenceChanged(PresenceUpdateDto update);
    Task UnreadNotification(UnreadNotificationDto notification);
    Task ReceiveMessage(MessageDto message);
    Task MessageEdited(MessageEditedDto edit);
    Task MessageDeleted(MessageDeletedDto deleted);
    Task InitialPresenceSync(IReadOnlyList<string> onlineUserIds);
    Task Error(string message);
}