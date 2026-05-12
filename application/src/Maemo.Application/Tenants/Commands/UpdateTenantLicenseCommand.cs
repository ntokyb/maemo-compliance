using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public sealed record UpdateTenantLicenseCommand(
    Guid TenantId,
    string SubscriptionPlan,
    int MaxUsers,
    long MaxStorageBytes,
    DateTime? SubscriptionExpiresAt,
    IReadOnlyList<string> EnabledModules) : IRequest<TenantLicenseSettingsDto>;
