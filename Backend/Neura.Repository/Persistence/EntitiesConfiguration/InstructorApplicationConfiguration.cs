using Neura.Core.Entities;
using Neura.Core.Enums;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class InstructorApplicationConfiguration : IEntityTypeConfiguration<InstructorApplication>
{
    public void Configure(EntityTypeBuilder<InstructorApplication> builder)
    {
        builder.ToTable("InstructorApplications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.ReviewedById)
            .HasMaxLength(450);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ApplicationStatus.Pending);

        builder.Property(x => x.Bio)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.Experience)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedOn)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships - Fix cascade delete issues
        builder.HasOne(x => x.User)
            .WithMany(u => u.InstructorApplications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Keep cascade - when user deleted, delete applications

        builder.HasOne(x => x.ReviewedBy)
            .WithMany()
            .HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction); // Changed to NoAction

        // Indexes
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.CreatedOn });
    }
}