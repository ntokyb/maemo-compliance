using MaemoCompliance.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

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
            .HasMaxLength(200);

        builder.Property(t => t.AdminEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.Edition)
            .HasMaxLength(50);

        builder.Property(t => t.Plan)
            .HasMaxLength(50);

        builder.Property(t => t.LicenseExpiryDate);

        builder.Property(t => t.ModulesEnabledJson)
            .HasMaxLength(500); // JSON array of module names

        builder.Property(t => t.SubscriptionId)
            .HasMaxLength(200);

        // Microsoft 365 Integration
        builder.Property(t => t.AzureAdTenantId)
            .HasMaxLength(200);

        builder.Property(t => t.AzureAdClientId)
            .HasMaxLength(200);

        builder.Property(t => t.AzureAdClientSecret)
            .HasMaxLength(500); // Will be encrypted later

        builder.Property(t => t.SharePointSiteId)
            .HasMaxLength(200);

        builder.Property(t => t.SharePointDriveId)
            .HasMaxLength(200);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.TrialEndsAt);

        // Onboarding
        builder.Property(t => t.OnboardingCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(t => t.OnboardingCompletedAt);

        builder.Property(t => t.OnboardingStepsCompletedJson);

        builder.Property(t => t.SetupComplete).HasDefaultValue(false);
        builder.Property(t => t.SetupStep).HasDefaultValue(0);
        builder.Property(t => t.TargetStandardsJson).HasMaxLength(2000);
        builder.Property(t => t.Industry).HasMaxLength(120);
        builder.Property(t => t.CompanySize).HasMaxLength(50);
        builder.Property(t => t.City).HasMaxLength(120);
        builder.Property(t => t.Province).HasMaxLength(120);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.ModifiedAt);

        builder.Property(t => t.ModifiedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Domain);
        builder.HasIndex(t => t.Plan);
    }
}

