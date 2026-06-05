using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.StripeSessionId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(p => p.StripeSessionId)
            .IsUnique();

        builder.Property(p => p.StripePaymentIntentId)
            .HasMaxLength(255);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.Status)
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(p => p.CreatedOn)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
