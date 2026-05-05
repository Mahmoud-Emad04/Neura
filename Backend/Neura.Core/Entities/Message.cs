namespace Neura.Core.Entities;

public sealed class Message
{
    public long Id { get; private set; }

    public string Content { get; private set; } = string.Empty;

    /// <summary>
    ///     Always stored in UTC. The client is responsible for local time conversion.
    /// </summary>
    public DateTime SentAt { get; private set; }

    /// <summary>
    ///     Null if the message has never been edited. Populated on every Edit() call.
    ///     The UI uses this to display the "(edited)" label.
    /// </summary>
    public DateTime? EditedAt { get; private set; }

    /// <summary>
    ///     Soft-delete flag. Hard deletes are never performed on messages.
    ///     The content is replaced with a tombstone string by the service layer.
    /// </summary>
    public bool IsDeleted { get; private set; }

    // -------------------------------------------------------------------------
    // Foreign Keys
    // -------------------------------------------------------------------------

    public int ChannelId { get; private set; }

    /// <summary>
    ///     String FK matches IdentityUser's default string PK on ApplicationUser.
    /// </summary>
    public string SenderId { get; private set; } = string.Empty;

    /// <summary>
    ///     Nullable self-reference for reply threading (future feature).
    ///     Stored now so the schema never needs to change.
    ///     DB constraint: ON DELETE SET NULL (parent deleted → reply orphaned, not cascaded).
    /// </summary>
    public long? ReplyToMessageId { get; private set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    public Channel Channel { get; private set; } = default!;

    /// <summary>
    ///     ⚠️ No reverse navigation (ICollection of Messages) on ApplicationUser.
    ///     Loading "all messages by a user" must go through the Messages DbSet
    ///     using the IX_Messages_SenderId index — never through User.Messages.
    /// </summary>
    public ApplicationUser Sender { get; private set; } = default!;

    public Message? ReplyToMessage { get; private set; }

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Required by EF Core. Never call directly from application code.
    /// </summary>
    private Message() { }

    // -------------------------------------------------------------------------
    // Factory Methods
    // -------------------------------------------------------------------------

    public static Message Create(
        int channelId,
        string senderId,
        string content,
        long? replyToMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(senderId);

        return new Message
        {
            ChannelId = channelId,
            SenderId = senderId,
            Content = content.Trim(),
            SentAt = DateTime.UtcNow,
            ReplyToMessageId = replyToMessageId
        };
    }

    // -------------------------------------------------------------------------
    // Domain Behavior
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Modifies message content and records the edit timestamp.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the message is already soft-deleted.</exception>
    public void Edit(string newContent)
    {
        if (IsDeleted)
            throw new InvalidOperationException("A deleted message cannot be edited.");

        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);

        Content = newContent.Trim();
        EditedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Marks the message as deleted. Content replacement with a tombstone
    ///     string (e.g., "[message deleted]") is the responsibility of the service layer.
    /// </summary>
    public void SoftDelete() => IsDeleted = true;
}