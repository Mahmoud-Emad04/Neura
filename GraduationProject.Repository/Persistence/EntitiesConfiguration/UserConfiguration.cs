using GraduationProject.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GraduationProject.Repository.Persistence.EntitiesConfiguration;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.OwnsMany(
            p => p.RefreshTokens,
            a =>
            {
                a.WithOwner().HasForeignKey("UserId");
                //a.Property<int>("Id");
                //a.HasKey("UserId", "Id");
            }
        );
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);
        builder.Property(u => u.DiscordHandle).HasMaxLength(100);
    }
}