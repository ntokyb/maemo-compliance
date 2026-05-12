using Maemo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class UserInvitationConfiguration : IEntityTypeConfiguration<UserInvitation>
{
    public void Configure(EntityTypeBuilder<UserInvitation> builder)
    {
        builder.ToTable("UserInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Role)
            .HasConversion<int>();

        builder.Property(x => x.InvitedByUserId)
            .HasMaxLength(200);

        builder.HasIndex(x => x.Token)
            .IsUnique();

        builder.HasIndex(x => new { x.TenantId, x.Email });
    }
}
