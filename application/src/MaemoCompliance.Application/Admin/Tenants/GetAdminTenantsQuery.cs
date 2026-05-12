using MediatR;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Query to get list of all tenants for admin view.
/// </summary>
public sealed record GetAdminTenantsQuery() : IRequest<IReadOnlyList<AdminTenantListItemDto>>;

