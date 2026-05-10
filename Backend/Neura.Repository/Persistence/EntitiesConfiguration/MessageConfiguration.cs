using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);

        // ── The cursor pagination index ────────────────────────────
        // Covers: WHERE ChannelId = @id AND Id < @cursor ORDER BY Id DESC
        // This is the critical index for GetMessageHistoryAsync performance.
        builder.HasIndex(m => new { m.ChannelId, m.Id })
               .IsDescending(false, true)
               .HasDatabaseName("IX_Messages_ChannelId_Id_Desc");

        // ── Sender lookup index ────────────────────────────────────
        builder.HasIndex(m => m.SenderId)
               .HasDatabaseName("IX_Messages_SenderId");

        // ── Content ────────────────────────────────────────────────
        builder.Property(m => m.Content)
               .IsRequired()
               .HasMaxLength(4_000);

        // ── Timestamps ─────────────────────────────────────────────
        builder.Property(m => m.SentAt)
               .IsRequired();

        builder.Property(m => m.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);

        // ── Soft delete filter ─────────────────────────────────────
        // Automatically excluded from all queries unless
        // .IgnoreQueryFilters() is explicitly called (admin/moderation).
        builder.HasQueryFilter(m => !m.IsDeleted);

        // ── Relationships ──────────────────────────────────────────
        builder.HasOne(m => m.Channel)
               .WithMany(c => c.Messages)
               .HasForeignKey(m => m.ChannelId);

        builder.HasOne(m => m.Sender)
               .WithMany()
               .HasForeignKey(m => m.SenderId);

        // Self-referencing reply chain — SET NULL on parent delete
        // so reply previews gracefully degrade to "deleted message"
        builder.HasOne(m => m.ReplyToMessage)
               .WithMany()
               .HasForeignKey(m => m.ReplyToMessageId);
    }
}
