using Maemo.Domain.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class WelcomeEmailConfiguration : IEntityTypeConfiguration<WelcomeEmail>
{
    public void Configure(EntityTypeBuilder<WelcomeEmail> builder)
    {
        builder.ToTable("WelcomeEmails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ToEmail)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Body)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.TenantId);
    }
}
