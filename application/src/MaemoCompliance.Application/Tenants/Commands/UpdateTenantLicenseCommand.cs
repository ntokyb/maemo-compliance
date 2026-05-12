using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public sealed record UpdateTenantLicenseCommand(
    Guid TenantId,
    string SubscriptionPlan,
    int MaxUsers,
    long MaxStorageBytes,
    DateTime? SubscriptionExpiresAt,
    IReadOnlyList<string> EnabledModules) : IRequest<TenantLicenseSettingsDto>;
