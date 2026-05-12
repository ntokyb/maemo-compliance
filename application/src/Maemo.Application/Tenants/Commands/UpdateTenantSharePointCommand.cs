using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public sealed record UpdateTenantSharePointCommand(
    Guid TenantId,
    string? SharePointSiteUrl,
    string? SharePointClientId,
    string? SharePointClientSecret,
    string? SharePointLibraryName) : IRequest<TenantSharePointSettingsDto>;
