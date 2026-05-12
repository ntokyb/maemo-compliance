using MediatR;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Query to get tenant detail for admin view.
/// </summary>
public sealed record GetAdminTenantDetailQuery(Guid Id) : IRequest<AdminTenantDetailDto?>;

