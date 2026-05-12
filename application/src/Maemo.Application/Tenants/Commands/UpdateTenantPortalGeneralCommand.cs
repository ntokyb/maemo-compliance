using MediatR;

namespace Maemo.Application.Tenants.Commands;

/// <summary>
/// Updates general tenant profile fields for the current tenant (portal / tenant admin).
/// </summary>
public sealed record UpdateTenantPortalGeneralCommand(
    string Name,
    string? LogoUrl,
    string? PrimaryColor) : IRequest;
