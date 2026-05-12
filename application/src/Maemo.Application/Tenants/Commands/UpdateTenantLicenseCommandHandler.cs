using Maemo.Application.Common;
using Maemo.Application.Tenants;
using Maemo.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Commands;

public class UpdateTenantLicenseCommandHandler : IRequestHandler<UpdateTenantLicenseCommand, TenantLicenseSettingsDto>
{
    private static readonly HashSet<string> KnownPlans =
    [
        "Starter", "Professional", "Enterprise", "GovOnPrem",
        "Pilot", "Standard" // legacy
    ];

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateTenantLicenseCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task<TenantLicenseSettingsDto> Handle(UpdateTenantLicenseCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {command.TenantId} not found.");
        }

        var plan = command.SubscriptionPlan.Trim();
        if (!KnownPlans.Contains(plan, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Subscription plan must be one of: Starter, Professional, Enterprise, GovOnPrem (or legacy Pilot, Standard).");
        }

        tenant.Plan = plan;
        if (string.Equals(plan, "GovOnPrem", StringComparison.OrdinalIgnoreCase))
        {
            tenant.Edition = "GovOnPrem";
        }
        else if (string.IsNullOrWhiteSpace(tenant.Edition) ||
                 string.Equals(tenant.Edition, "GovOnPrem", StringComparison.OrdinalIgnoreCase))
        {
            tenant.Edition = "Standard";
        }

        tenant.MaxUsers = command.MaxUsers < 1 ? 1 : command.MaxUsers;
        tenant.MaxStorageBytes = command.MaxStorageBytes < 0 ? 0 : command.MaxStorageBytes;
        tenant.LicenseExpiryDate = command.SubscriptionExpiresAt;

        var modules = NormalizeModules(command.EnabledModules);
        tenant.SetEnabledModules(modules);

        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _businessAuditLogger.LogAsync(
            "Tenant.LicenseUpdated",
            "Tenant",
            tenant.Id.ToString(),
            new
            {
                tenant.Plan,
                tenant.Edition,
                tenant.MaxUsers,
                tenant.MaxStorageBytes,
                tenant.LicenseExpiryDate,
                Modules = modules
            },
            cancellationToken);

        return new TenantLicenseSettingsDto
        {
            SubscriptionPlan = tenant.Plan,
            Edition = tenant.Edition,
            MaxUsers = tenant.MaxUsers,
            MaxStorageBytes = tenant.MaxStorageBytes,
            SubscriptionExpiresAt = tenant.LicenseExpiryDate,
            EnabledModules = tenant.GetEnabledModules().ToArray()
        };
    }

    private static List<string> NormalizeModules(IReadOnlyList<string> enabledModules)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["documents"] = "Documents",
            ["document"] = "Documents",
            ["ncr"] = "NCR",
            ["ncrs"] = "NCR",
            ["risks"] = "Risks",
            ["risk"] = "Risks",
            ["audits"] = "Audits",
            ["audit"] = "Audits"
        };

        var result = new List<string>();
        foreach (var raw in enabledModules)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var key = raw.Trim();
            if (map.TryGetValue(key, out var canonical))
            {
                if (!result.Contains(canonical, StringComparer.Ordinal))
                {
                    result.Add(canonical);
                }

                continue;
            }

            var title = char.ToUpperInvariant(key[0]) + key[1..];
            if (!result.Contains(title, StringComparer.Ordinal))
            {
                result.Add(title);
            }
        }

        return result;
    }
}
