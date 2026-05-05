namespace Neura.Core.Hubs;

public static class HubEvents
{
    // -------------------------------------------------------------------------
    // Course-level events (broadcast to course-{id} group)
    // Payload: lightweight DTOs only
    // -------------------------------------------------------------------------

    /// <summary>Payload: <see cref="PresenceUpdateDto"/></summary>
    public const string PresenceChanged = "PresenceChanged";

    /// <summary>
    ///     Signals that a channel has at least one new message the
    ///     user hasn't seen yet — drives the unread badge in the sidebar.
    ///     Payload: <see cref="UnreadNotificationDto"/>
    /// </summary>
    public const string UnreadNotification = "UnreadNotification";

    // -------------------------------------------------------------------------
    // Channel-level events (broadcast to channel-{id} group)
    // Payload: heavy MessageDto payloads
    // -------------------------------------------------------------------------

    /// <summary>Payload: <see cref="MessageDto"/></summary>
    public const string ReceiveMessage = "ReceiveMessage";

    /// <summary>Payload: <see cref="MessageEditedDto"/></summary>
    public const string MessageEdited = "MessageEdited";

    /// <summary>Payload: <see cref="MessageDeleted"/></summary>
    public const string MessageDeleted = "MessageDeleted";

    // -------------------------------------------------------------------------
    // Connection lifecycle events (sent to caller only)
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Sent to the connecting client immediately after a successful handshake.
    ///     Payload: list of online userIds so the UI can hydrate the member list
    ///     without a separate REST call.
    /// </summary>
    public const string InitialPresenceSync = "InitialPresenceSync";

    /// <summary>
    ///     Sent to the caller when an error occurs inside a Hub method.
    ///     Payload: string error message.
    ///     Avoids throwing <see cref="HubException"/> for non-fatal errors
    ///     so the client connection stays alive.
    /// </summary>
    public const string Error = "Error";
}
