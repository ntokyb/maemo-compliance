using MaemoCompliance.Application.Billing;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Onboarding;
using MaemoCompliance.Application.Onboarding.Dtos;
using MaemoCompliance.Domain.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaemoCompliance.Application.Tenants.Commands;

public class ProvisionTenantCommandHandler : IRequestHandler<ProvisionTenantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBillingProvider _billingProvider;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IEncryptionService? _encryptionService;
    private readonly IOnboardingSeeder _onboardingSeeder;

    public ProvisionTenantCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        IBillingProvider billingProvider,
        IDeploymentContext deploymentContext,
        IOnboardingSeeder onboardingSeeder,
        IEncryptionService? encryptionService = null)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _billingProvider = billingProvider;
        _deploymentContext = deploymentContext;
        _onboardingSeeder = onboardingSeeder;
        _encryptionService = encryptionService;
    }

    public async Task<Guid> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var validPlans = new[]
        {
            "Free", "Starter", "Professional", "Enterprise",
            "Standard", "Pilot"
        };
        if (!validPlans.Contains(command.Plan, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Plan must be one of: {string.Join(", ", validPlans)}. Provided: {command.Plan}",
                nameof(command.Plan));
        }

        // Check if tenant with same name already exists
        var existingTenant = await _context.Tenants
            .Where(t => t.Name == command.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTenant != null)
        {
            throw new InvalidOperationException(
                $"Tenant with name '{command.Name}' already exists.");
        }

        var normalizedPlan = command.Plan.Trim();
        string edition = "Standard";
        if (string.Equals(normalizedPlan, "Enterprise", StringComparison.OrdinalIgnoreCase))
        {
            edition = "Enterprise";
        }

        var modulesJson = command.EnableDefaultComplianceModules
            ? JsonSerializer.Serialize(new[] { "Documents", "NCR", "Risks", "Audits" })
            : "[]";

        // Create new tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            AdminEmail = command.AdminEmail,
            Plan = normalizedPlan,
            Edition = edition,
            IsActive = true,
            CreatedAt = now,
            TrialEndsAt = command.TrialEndsAt,
            ModulesEnabledJson = modulesJson,
            CreatedBy = "System"
        };

        _context.Tenants.Add(tenant);

        var skipBilling = string.Equals(normalizedPlan, "Free", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(normalizedPlan, "Starter", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(normalizedPlan, "Pilot", StringComparison.OrdinalIgnoreCase);

        // Create subscription with billing provider (optional plans)
        if (!skipBilling)
        {
            try
            {
                var subscriptionId = await _billingProvider.CreateSubscriptionAsync(
                    tenant.Id, 
                    command.Plan, 
                    command.AdminEmail);
                
                // Encrypt subscription ID in GovOnPrem mode
                if (_deploymentContext.IsGovOnPrem)
                {
                    if (_encryptionService == null)
                    {
                        throw new InvalidOperationException(
                            "Encryption service is required for GovOnPrem mode but is not configured. " +
                            "Please configure Security:EncryptionKey in appsettings.");
                    }
                    tenant.SubscriptionId = _encryptionService.Encrypt(subscriptionId);
                }
                else
                {
                    tenant.SubscriptionId = subscriptionId;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail tenant creation
                // Note: We log tenant ID and plan, but NOT subscription details or admin email
                throw new InvalidOperationException(
                    $"Failed to create subscription for tenant {tenant.Id} with plan {command.Plan}. " +
                    "See inner exception for details.", ex);
            }
        }

        // Create default departments
        var defaultDepartments = new[]
        {
            new Department
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Operations",
                Description = "Operations department",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "System"
            },
            new Department
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Quality Assurance",
                Description = "Quality Assurance department",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "System"
            },
            new Department
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Compliance",
                Description = "Compliance department",
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "System"
            }
        };

        foreach (var department in defaultDepartments)
        {
            _context.Departments.Add(department);
        }

        // Create default compliance settings
        var defaultSettings = new[]
        {
            new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Key = "DocumentReviewReminderDays",
                Value = "14",
                Description = "Number of days before document review date to send reminder",
                CreatedAt = now,
                CreatedBy = "System"
            },
            new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Key = "NcrDefaultDueDays",
                Value = "30",
                Description = "Default number of days for NCR due date",
                CreatedAt = now,
                CreatedBy = "System"
            },
            new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Key = "HighRiskThreshold",
                Value = "15",
                Description = "Residual score threshold for high risk classification",
                CreatedAt = now,
                CreatedBy = "System"
            }
        };

        foreach (var setting in defaultSettings)
        {
            _context.TenantSettings.Add(setting);
        }

        if (!string.IsNullOrWhiteSpace(command.Industry))
        {
            _context.TenantSettings.Add(new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Key = "SignupIndustry",
                Value = command.Industry.Trim(),
                Description = "Industry captured at signup",
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var iso = command.IsoFrameworks?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList()
                  ?? new List<string>();
        if (iso.Count > 0)
        {
            var industryKey = (command.Industry ?? "Other").Trim();
            await _onboardingSeeder.SeedForTenantAsync(
                tenant.Id,
                new OnboardingRequest
                {
                    IsoStandards = iso,
                    Industry = industryKey,
                    CompanySize = "medium"
                },
                cancellationToken);
        }

        return tenant.Id;
    }
}

