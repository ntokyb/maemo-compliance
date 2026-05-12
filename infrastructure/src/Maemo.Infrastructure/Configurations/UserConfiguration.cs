using Maemo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.TenantId);

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("datetime2");

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.ModifiedAt);

        builder.Property(u => u.ModifiedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => u.Role);
        builder.HasIndex(u => u.IsActive);
    }
}

