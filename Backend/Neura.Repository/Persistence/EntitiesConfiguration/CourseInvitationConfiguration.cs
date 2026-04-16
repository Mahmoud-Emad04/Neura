using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class CourseInvitationConfiguration : IEntityTypeConfiguration<CourseInvitation>
{
    public void Configure(EntityTypeBuilder<CourseInvitation> builder)
    {
        builder.ToTable("CourseInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(InvitationStatus.Pending);

        builder.Property(x => x.CustomMessage)
            .HasMaxLength(500);

        builder.Property(x => x.InvitedById)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.AcceptedUserId)
            .HasMaxLength(450);

        builder.Property(x => x.InvitedOn)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.ExpiresOn)
            .IsRequired();

        // Relationships - Fix cascade delete issues
        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Keep cascade for course

        builder.HasOne(x => x.CourseRole)
            .WithMany()
            .HasForeignKey(x => x.CourseRoleId)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        builder.HasOne(x => x.InvitedBy)
            .WithMany(u => u.SentInvitations)
            .HasForeignKey(x => x.InvitedById)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        builder.HasOne(x => x.AcceptedUser)
            .WithMany(u => u.ReceivedInvitations)
            .HasForeignKey(x => x.AcceptedUserId)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        // Indexes
        builder.HasIndex(x => x.Token)
            .IsUnique();

        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => new { x.CourseId, x.Status });
        builder.HasIndex(x => new { x.CourseId, x.Email, x.Status });
    }
}