using Neura.Core.Enums;

namespace Neura.Core.Entities;

public sealed class Channel : AuditableEntity
{
    public int Id { get; private set; }

    /// <summary>
    ///     Lowercase, trimmed channel name shown in the sidebar (e.g., "general", "help-react").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     Optional description shown at the top of the chat view.
    /// </summary>
    public string? Topic { get; private set; }

    /// <summary>
    ///     Determines UI behavior and Hub routing logic (Text = real-time chat, Voice = future WebRTC).
    /// </summary>
    public ChannelType Type { get; private set; }

    /// <summary>
    ///     Zero-based display order within the course sidebar.
    ///     Indexed alongside CourseId for fast sidebar queries.
    /// </summary>
    public int Position { get; private set; }

    public bool IsDeleted { get; private set; }

    // -------------------------------------------------------------------------
    // Foreign Keys
    // -------------------------------------------------------------------------

    public int CourseId { get; private set; }

    // -------------------------------------------------------------------------
    // Navigation Properties
    // -------------------------------------------------------------------------

    public Course Course { get; private set; } = default!;

    /// <summary>
    ///     Only populated for Text channels. Voice channels will never have messages.
    ///     Do NOT load this collection without projection — it is unbounded.
    /// </summary>
    public ICollection<Message> Messages { get; private set; } = [];

    // -------------------------------------------------------------------------
    // Constructors
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Required by EF Core. Never call directly from application code.
    /// </summary>
    private Channel() { }

    // -------------------------------------------------------------------------
    // Factory Methods (the ONLY valid way to create a Channel)
    // -------------------------------------------------------------------------

    public static Channel Create(
        int courseId,
        string name,
        ChannelType type,
        int position,
        string? topic = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Channel
        {
            CourseId = courseId,
            Name = name.Trim().ToLowerInvariant(),
            Type = type,
            Position = position,
            Topic = topic?.Trim()
        };
    }

    // -------------------------------------------------------------------------
    // Domain Behavior
    // -------------------------------------------------------------------------

    public void UpdateDetails(string name, string? topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim().ToLowerInvariant();
        Topic = topic?.Trim();
    }

    /// <summary>
    ///     Called during bulk reorder operations (e.g., drag-and-drop in sidebar).
    /// </summary>
    public void Reorder(int newPosition) => Position = newPosition;

    public void Delete() => IsDeleted = true;
}