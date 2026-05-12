using MediatR;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Command to update tenant status in admin view.
/// </summary>
public sealed record UpdateAdminTenantStatusCommand(Guid TenantId, string Status) : IRequest;

