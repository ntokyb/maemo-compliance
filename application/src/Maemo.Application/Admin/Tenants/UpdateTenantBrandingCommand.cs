using MediatR;

namespace Maemo.Application.Admin.Tenants;

/// <summary>
/// Command to update tenant branding (logo URL and primary color).
/// </summary>
public sealed record UpdateTenantBrandingCommand(
    Guid TenantId,
    string? LogoUrl,
    string? PrimaryColor
) : IRequest;

