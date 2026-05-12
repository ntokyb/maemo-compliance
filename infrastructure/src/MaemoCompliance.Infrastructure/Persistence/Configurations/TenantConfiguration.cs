using MaemoCompliance.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Domain)
            .HasMaxLength(255);

        builder.Property(t => t.AdminEmail)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Licensing & Plans
        builder.Property(t => t.Edition)
            .HasMaxLength(100)
            .HasDefaultValue("Standard");

        builder.Property(t => t.Plan)
            .HasMaxLength(100)
            .HasDefaultValue("Pilot");

        builder.Property(t => t.LicenseExpiryDate)
            .HasColumnType("datetime2");

        builder.Property(t => t.ModulesEnabledJson)
            .HasColumnType("nvarchar(max)")
            .HasDefaultValue("[]");

        builder.Property(t => t.SubscriptionId)
            .HasMaxLength(200);

        builder.Property(t => t.TrialEndsAt)
            .HasColumnType("datetime2");

        // Branding
        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);

        builder.Property(t => t.PrimaryColor)
            .HasMaxLength(50); // Hex color code

        // Microsoft 365 Integration
        builder.Property(t => t.AzureAdTenantId)
            .HasMaxLength(200);

        builder.Property(t => t.AzureAdClientId)
            .HasMaxLength(200);

        builder.Property(t => t.AzureAdClientSecret)
            .HasMaxLength(500); // Will be encrypted

        builder.Property(t => t.SharePointSiteId)
            .HasMaxLength(200);

        builder.Property(t => t.SharePointDriveId)
            .HasMaxLength(200);

        builder.Property(t => t.SharePointSiteUrl)
            .HasMaxLength(500);

        builder.Property(t => t.SharePointLibraryName)
            .HasMaxLength(200)
            .HasDefaultValue("Shared Documents");

        builder.Property(t => t.SharePointClientId)
            .HasMaxLength(200);

        builder.Property(t => t.SharePointClientSecretEncrypted)
            .HasMaxLength(1000);

        builder.Property(t => t.MaxUsers)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(t => t.MaxStorageBytes)
            .IsRequired()
            .HasDefaultValue(5_368_709_120L);

        // Indexes
        builder.HasIndex(t => t.Domain)
            .HasDatabaseName("IX_Tenants_Domain");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_Tenants_IsActive");

        builder.HasIndex(t => t.Edition)
            .HasDatabaseName("IX_Tenants_Edition");

        builder.HasIndex(t => t.Plan)
            .HasDatabaseName("IX_Tenants_Plan");

        builder.Property(t => t.OnboardingStepsCompletedJson)
            .HasColumnType("nvarchar(max)");
    }
}

