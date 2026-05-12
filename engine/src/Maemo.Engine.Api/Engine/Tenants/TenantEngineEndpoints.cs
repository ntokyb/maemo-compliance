using Maemo.Application.Common;
using Maemo.Application.Engine;
using Maemo.Application.Security;
using Maemo.Application.Tenants.Dtos;
using Maemo.Shared.Contracts.Engine.Tenants;
using CreateApiKeyRequest = Maemo.Shared.Contracts.Engine.Tenants.CreateApiKeyRequest;
using CreateApiKeyResponse = Maemo.Shared.Contracts.Engine.Tenants.CreateApiKeyResponse;
using ApiKeyDto = Maemo.Shared.Contracts.Engine.Tenants.ApiKeyDto;
using UpdateTenantSettingsRequest = Maemo.Shared.Contracts.Engine.Tenants.UpdateTenantSettingsRequest;

namespace Maemo.Engine.Api.Engine.Tenants;

/// <summary>
/// Tenants Engine API endpoints - Tenant configuration and API key management for the Compliance Engine.
/// </summary>
public static class TenantEngineEndpoints
{
    /// <summary>
    /// Maps all Tenants Engine endpoints under /engine/v1/tenants route group.
    /// </summary>
    public static RouteGroupBuilder MapTenantEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var tenantsGroup = engineGroup
            .MapGroup("/tenants")
            .WithTags("Engine - Tenants")
            .WithDescription("Tenant configuration and API key management for programmatic access")
            .RequireAuthorization("RequireAdminOrTenantAdmin");

        // GET /engine/v1/tenants/{id}
        tenantsGroup.MapGet("/{id:guid}", async (
            Guid id,
            ITenantEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var tenant = await engine.GetAsync(id, cancellationToken);
                if (tenant == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(tenant);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .WithName("EngineV1_GetTenantById")
        .WithOpenApi()
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /engine/v1/tenants/{id}
        tenantsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTenantSettingsRequest request,
            ITenantEngine engine,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await engine.UpdateSettingsAsync(id, request, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_UpdateTenantSettings")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // POST /engine/v1/tenants/{tenantId}/apikeys
        tenantsGroup.MapPost("/{tenantId:guid}/apikeys", async (
            Guid tenantId,
            CreateApiKeyRequest request,
            IApiKeyService apiKeyService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var apiKey = await apiKeyService.CreateAsync(tenantId, request.Name, cancellationToken);
                var response = new CreateApiKeyResponse
                {
                    Id = apiKey.Id,
                    TenantId = apiKey.TenantId,
                    Key = apiKey.Key, // Only returned on creation
                    Name = apiKey.Name,
                    CreatedAt = apiKey.CreatedAt
                };
                return Results.Created($"/engine/v1/tenants/{tenantId}/apikeys/{apiKey.Id}", response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_CreateApiKey")
        .WithSummary("Create API key")
        .WithDescription("Creates a new API key for programmatic access to the Engine API. The key value is only returned on creation - store it securely.")
        .WithOpenApi()
        .Produces<CreateApiKeyResponse>(StatusCodes.Status201Created, "application/json")
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // GET /engine/v1/tenants/{tenantId}/apikeys
        tenantsGroup.MapGet("/{tenantId:guid}/apikeys", async (
            Guid tenantId,
            IApiKeyService apiKeyService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var apiKeys = await apiKeyService.GetByTenantAsync(tenantId, cancellationToken);
                var dtos = apiKeys.Select(k => new ApiKeyDto
                {
                    Id = k.Id,
                    TenantId = k.TenantId,
                    Name = k.Name,
                    IsActive = k.IsActive,
                    CreatedAt = k.CreatedAt
                }).ToList();
                return Results.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetApiKeys")
        .WithOpenApi()
        .Produces<IReadOnlyList<ApiKeyDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /engine/v1/tenants/{tenantId}/apikeys/{keyId}
        tenantsGroup.MapDelete("/{tenantId:guid}/apikeys/{keyId:guid}", async (
            Guid tenantId,
            Guid keyId,
            IApiKeyService apiKeyService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await apiKeyService.RevokeAsync(keyId, cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_RevokeApiKey")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        return engineGroup;
    }
}

