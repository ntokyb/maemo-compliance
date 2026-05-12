using MaemoCompliance.Api.Authentication;
using MaemoCompliance.Api.Portal;
using MaemoCompliance.Api.Public;
using MaemoCompliance.Api.Admin;
using MaemoCompliance.Api.Development;
using MaemoCompliance.Api.Engine;
using MaemoCompliance.Api.Middleware;
using MaemoCompliance.Application;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Infrastructure;
using MaemoCompliance.Infrastructure.HealthChecks;
using MaemoCompliance.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using MaemoCompliance.Infrastructure.Logging;
using Serilog;
using System.Text.Json;

// Configure initial logger (will be replaced after full configuration)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Maemo Compliance API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with security-focused settings
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogConfiguration.ConfigureSerilog(configuration, context.Configuration, services);
    });

    // Add services
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger/OpenAPI
    var swaggerDeploymentContext = new MaemoCompliance.Infrastructure.Common.DeploymentContext(builder.Configuration);
    var isGovOnPrem = swaggerDeploymentContext.Mode == MaemoCompliance.Domain.Common.DeploymentMode.GovOnPrem;
    
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Maemo Compliance Engine API",
            Version = "v1",
            Description = @"
# Maemo Compliance Engine API

The Maemo Compliance Engine provides a comprehensive API-first consumption layer for governance, risk, and compliance operations.

## Authentication

All endpoints require authentication via one of the following methods:

- **JWT Bearer Token**: Standard OAuth2/JWT authentication for user-based access
- **API Key**: Programmatic access using `X-Api-Key` header (see `/engine/v1/tenants/{tenantId}/apikeys`)

## Tenant Scoping

All operations are automatically scoped to the authenticated tenant:
- When using JWT authentication, tenant context is derived from the user's claims
- When using API Key authentication, tenant context is automatically set from the API key's tenant

## Engine Endpoints

The `/engine/v1` endpoints provide a stable, versioned API surface designed for external consumption and integration.

### Available Modules:
- **Documents**: Document lifecycle management and versioning
- **NCR**: Non-Conformance Report management and tracking
- **Risks**: Risk register and assessment operations
- **Audits**: Audit template and run management
- **Consultants**: Consultant-specific operations and dashboards
- **Tenants**: Tenant configuration and API key management
- **Webhooks**: Webhook subscription management for event notifications

## Portal Endpoints

The `/api` endpoints are designed for internal Maemo Portal use and may change without notice.
For external integrations, always use `/engine/v1` endpoints.
"
        });

        // Add JWT Bearer authentication scheme
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        // Add API Key authentication scheme
        options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "API Key authentication for programmatic access. Include the API key in the X-Api-Key header.",
            Name = "X-Api-Key",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
        });

        // Apply security requirements
        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            },
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Ensure unique schema IDs to prevent conflicts
        options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
        
        // Group endpoints by tags
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor?.DisplayName ?? "Default" });
        
        // Exclude Portal API endpoints from Swagger (they're for internal use)
        // Only include Engine API and Admin API endpoints in Swagger documentation
        // This is the PRIMARY filter to prevent Portal endpoints from appearing in Swagger
        options.DocInclusionPredicate((name, api) =>
        {
            var path = api.RelativePath ?? "";
            var displayName = api.ActionDescriptor?.DisplayName ?? "";
            
            // Exclude Portal API endpoints - they start with "api/" in RelativePath
            // For Minimal APIs, RelativePath is the route template without leading slash
            if (path.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
            {
                return false; // Exclude Portal API
            }
            
            // Also exclude if display name suggests Portal endpoint
            if (!string.IsNullOrEmpty(displayName) && 
                !displayName.Contains("Engine") && 
                !displayName.Contains("Admin") &&
                path.Contains("/api/"))
            {
                return false;
            }
            
            // Include Engine API, Admin API, and health checks only
            return path.StartsWith("engine/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("admin/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("health", StringComparison.OrdinalIgnoreCase);
        });
        
        // Ensure unique operation IDs to prevent conflicts
        // CRITICAL: Include route group/prefix in operation ID to ensure uniqueness across different route groups
        // This prevents conflicts when the same endpoint name exists in Portal, Engine, and Admin APIs
        options.CustomOperationIds(apiDesc =>
        {
            var path = apiDesc.RelativePath ?? "";
            var method = apiDesc.HttpMethod ?? "GET";
            var groupName = apiDesc.GroupName ?? "";
            
            // Try to get the endpoint name from RouteEndpointMetadata
            var endpointName = apiDesc.ActionDescriptor?.DisplayName;
            
            // Determine route prefix to include in operation ID for uniqueness
            string routePrefix = "";
            if (path.StartsWith("engine/", StringComparison.OrdinalIgnoreCase))
            {
                routePrefix = "EngineV1_";
            }
            else if (path.StartsWith("admin/", StringComparison.OrdinalIgnoreCase))
            {
                routePrefix = "AdminV1_";
            }
            else if (path.StartsWith("health", StringComparison.OrdinalIgnoreCase))
            {
                routePrefix = "Health_";
            }
            else if (!string.IsNullOrEmpty(groupName))
            {
                routePrefix = $"{groupName.Replace(" ", "_")}_";
            }
            
            if (!string.IsNullOrEmpty(endpointName))
            {
                // Use the endpoint name with route prefix for uniqueness
                var cleanName = endpointName.Replace(" ", "").Replace("/", "_").Replace("-", "_");
                // Check if name already includes prefix (some endpoints like EngineV1_CreateRisk already have it)
                if (cleanName.StartsWith(routePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return cleanName;
                }
                return $"{routePrefix}{cleanName}";
            }
            
            // Fallback: generate from path and method with route prefix
            var cleanPath = path.Replace("/", "_").Replace("{", "").Replace("}", "").Replace(":", "").Replace("-", "_");
            var operationId = $"{routePrefix}{method}_{cleanPath}";
            return operationId;
        });
        
        // Resolve conflicts when multiple endpoints have the same path/method
        // Safe guardrail to prevent Swagger crashes - takes first matching endpoint
        // This handles conflicts at the ApiDescription level before metadata generation
        // CRITICAL: This prevents "Sequence contains more than one matching element" errors
        // Enhanced with more aggressive conflict resolution and defensive programming
        options.ResolveConflictingActions(apiDescriptions =>
        {
            // Defensive: Handle null or empty collections
            if (apiDescriptions == null)
            {
                throw new InvalidOperationException("Swagger ResolveConflictingActions received null apiDescriptions");
            }
            
            var descriptions = apiDescriptions.ToList();
            
            // Defensive: Handle empty collection
            if (descriptions.Count == 0)
            {
                throw new InvalidOperationException("Swagger ResolveConflictingActions received empty apiDescriptions collection");
            }
            
            // If there's only one, return it immediately
            if (descriptions.Count == 1)
            {
                return descriptions.First();
            }
            
            // Log conflict for debugging (only in Development)
            if (builder.Environment.IsDevelopment())
            {
                var conflictDetails = string.Join(" | ", descriptions.Select(d => 
                    $"{d.HttpMethod ?? "UNKNOWN"} {d.RelativePath ?? "NULL"} (Group: {d.GroupName ?? "None"}, Name: {d.ActionDescriptor?.DisplayName ?? "None"})"));
                Log.Warning("Swagger conflict detected for {Count} endpoints: {Details}. Selecting preferred endpoint.", 
                    descriptions.Count, conflictDetails);
            }
            
            // Multiple matches - prefer Engine endpoints, then Admin endpoints, then health checks
            // Use FirstOrDefault to safely handle nulls with defensive null checks
            var preferred = descriptions
                .Where(x => x != null && !string.IsNullOrEmpty(x.RelativePath))
                .FirstOrDefault(x => x.RelativePath!.StartsWith("engine/", StringComparison.OrdinalIgnoreCase)) 
                ?? descriptions
                .Where(x => x != null && !string.IsNullOrEmpty(x.RelativePath))
                .FirstOrDefault(x => x.RelativePath!.StartsWith("admin/", StringComparison.OrdinalIgnoreCase))
                ?? descriptions
                .Where(x => x != null && !string.IsNullOrEmpty(x.RelativePath))
                .FirstOrDefault(x => x.RelativePath!.StartsWith("health", StringComparison.OrdinalIgnoreCase))
                ?? descriptions.FirstOrDefault(x => x != null) // Fallback to first non-null
                ?? descriptions.First(); // Last resort - should never happen
            
            // Final defensive check
            if (preferred == null)
            {
                throw new InvalidOperationException($"Swagger ResolveConflictingActions failed to select a preferred endpoint from {descriptions.Count} candidates");
            }
            
            return preferred;
        });
        
        // Ignore obsolete actions to reduce conflicts
        options.IgnoreObsoleteActions();
        
        // Ignore obsolete properties in schemas
        options.IgnoreObsoleteProperties();
        
        // CRITICAL: Ensure ApiExplorer is configured to handle conflicts properly
        // This helps Swashbuckle identify and resolve conflicts earlier in the pipeline
        options.SupportNonNullableReferenceTypes();
    });

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev", policy =>
        {
            // Allow both Portal (4200) and Admin Console (4300) in development
            policy.WithOrigins("http://localhost:4200", "http://localhost:4300")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configure Authentication
    builder.Services
        .AddAuthentication(options =>
        {
            // Default scheme is JWT Bearer
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
            // Don't fail if no token is present - allow API Key auth to be tried
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // If authentication fails and no token was provided, don't fail
                    // This allows API Key authentication to be tried
                    if (string.IsNullOrEmpty(context.Request.Headers.Authorization))
                    {
                        context.NoResult();
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

    // Add Authorization with role-based policies
    builder.Services.AddAuthorization(options =>
    {
        // RequireAdmin policy - user must have Admin role
        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "Admin" || c.Value == "0"))));

        // PlatformAdmin policy - for Codist staff only, requires PlatformAdmin role or specific claim
        // This is more restrictive than RequireAdmin and is used for internal platform operations
        options.AddPolicy("PlatformAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "PlatformAdmin" || c.Value == "CodistAdmin"))));

        // RequireTenantAdmin policy - user must have TenantAdmin role
        options.AddPolicy("RequireTenantAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "TenantAdmin" || c.Value == "1"))));

        // RequireConsultant policy - user must have Consultant role
        options.AddPolicy("RequireConsultant", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "Consultant" || c.Value == "3"))));

        // RequireAdminOrTenantAdmin policy - user must have Admin or TenantAdmin role
        options.AddPolicy("RequireAdminOrTenantAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "Admin" || c.Value == "0" || c.Value == "TenantAdmin" || c.Value == "1"))));

        // EngineClients policy - allows either JWT user auth OR API Key auth
        options.AddPolicy("EngineClients", policy =>
            policy.RequireAssertion(context =>
            {
                // Allow if authenticated via API Key (has ApiKeyClient role)
                if (context.User.HasClaim(c => 
                    c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "ApiKeyClient"))
                {
                    return true;
                }

                // Allow if authenticated via JWT (has any role claim)
                return context.User.HasClaim(c => 
                    c.Type == System.Security.Claims.ClaimTypes.Role || 
                    c.Type == "role" || 
                    c.Type == "roles");
            }));
    });

    // Add Application services (MediatR)
    builder.Services.AddApplication();

    // Add Infrastructure services
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>(
            "database",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "ready", "db" })
        .AddCheck<FileStorageHealthCheck>(
            "file_storage",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "ready", "storage" });

    var app = builder.Build();
    
    // Log deployment mode after services are registered
    var deploymentContext = app.Services.GetRequiredService<MaemoCompliance.Application.Common.IDeploymentContext>();
    Log.Information("Maemo Compliance API running in {Mode} mode", deploymentContext.Mode);

    // Apply migrations in Development environment
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
            context.Database.Migrate();
        }
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        // Enable CORS for development
        app.UseCors("AllowAngularDev");
    }

    // Enable Swagger/OpenAPI in all environments except strict GovOnPrem
    // For GovOnPrem, Swagger can be enabled via configuration if needed
    var enableSwagger = !isGovOnPrem || app.Environment.IsDevelopment();
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Maemo Compliance Engine API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Maemo Compliance Engine API";
            options.DefaultModelsExpandDepth(-1); // Collapse models by default
            options.DisplayRequestDuration();
        });
    }

    // Only use HTTPS redirection if running on HTTPS
    // Comment out for development when using HTTP
    // app.UseHttpsRedirection();

    // Security headers middleware (before authentication)
    var deploymentContextForMiddleware = app.Services.GetRequiredService<MaemoCompliance.Application.Common.IDeploymentContext>();
    app.UseMiddleware<SecurityHeadersMiddleware>(deploymentContextForMiddleware);

    // Health check security middleware (before authentication, for GovOnPrem restrictions)
    app.UseMiddleware<HealthCheckSecurityMiddleware>();

    // Authentication & Authorization middleware (must be before endpoints)
    app.UseAuthentication();
    app.UseAuthorization();

    // Global exception handler (must be early in pipeline to catch all exceptions)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    
    // Tenant middleware (after authentication, before endpoints)
    app.UseMiddleware<TenantMiddleware>();
    app.UseMiddleware<TenantGatekeepingMiddleware>();
    
    // Logging middleware (after tenant middleware, before endpoints)
    app.UseMiddleware<ApiCallLoggingMiddleware>();
    app.UseMiddleware<ErrorLoggingMiddleware>();

    // Health check endpoints
    // Liveness probe - simple check that app is running
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // Don't run any checks, just return OK
        ResponseWriter = async (context, report) =>
        {
            var deploymentContext = app.Services.GetRequiredService<IDeploymentContext>();
            context.Response.ContentType = "application/json";
            
            var isGovOnPrem = deploymentContext.IsGovOnPrem;
            var response = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        },
        AllowCachingResponses = false
    })
    .AllowAnonymous()
    .WithName("HealthLive")
    .WithTags("health");

    // Readiness probe - checks database, storage, etc.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            var deploymentContext = app.Services.GetRequiredService<IDeploymentContext>();
            context.Response.ContentType = "application/json";
            
            var isGovOnPrem = deploymentContext.IsGovOnPrem;
            var status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy";
            
            // In GovOnPrem mode, don't expose detailed error information
            if (isGovOnPrem)
            {
                var response = new
                {
                    status = status,
                    timestamp = DateTime.UtcNow
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            else
            {
                // In SaaS mode, include more details (but still sanitized)
                var response = new
                {
                    status = status,
                    timestamp = DateTime.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description
                    })
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        },
        AllowCachingResponses = false
    })
    .AllowAnonymous()
    .WithName("HealthReady")
    .WithTags("health");

    // Legacy health endpoint (kept for backward compatibility)
    app.MapGet("/api/health", () =>
    {
        return Results.Ok(new
        {
            status = "OK",
            timestamp = DateTime.UtcNow
        });
    })
    .AllowAnonymous()
    .WithName("GetHealth")
    .WithOpenApi();

    // Current tenant endpoint
    app.MapGet("/api/tenants/current", (ITenantProvider tenantProvider) =>
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        return Results.Ok(new
        {
            tenantId = tenantId
        });
    })
    .WithName("GetCurrentTenant")
    .WithOpenApi();

    // Secure test endpoint
    app.MapGet("/api/secure-test", (ICurrentUserService currentUserService) =>
    {
        return Results.Ok(new
        {
            userId = currentUserService.UserId,
            message = "This is a secure endpoint"
        });
    })
    .RequireAuthorization()
    .WithName("GetSecureTest")
    .WithOpenApi();

    // Demo endpoints
    app.MapDemoEndpoints();

    // Document endpoints
    app.MapDocumentsEndpoints();
    app.MapPopiaTrailEndpoints();

    // NCR endpoints
    app.MapNcrsEndpoints();

    // Risk endpoints
    app.MapRisksEndpoints();

    app.MapPublicSignupEndpoints();

    // Tenant endpoints (Portal)
    app.MapTenantsEndpoints();
    app.MapTenantSelfServiceEndpoints();

    // Onboarding endpoints (Portal)
    app.MapOnboardingEndpoints();

    // Billing endpoints (Portal - webhooks)
    app.MapBillingEndpoints();

    // Dashboard endpoints
    app.MapDashboardEndpoints();

    // Consultant endpoints
    app.MapConsultantsEndpoints();

    // Audit endpoints
    app.MapAuditsEndpoints();

    // Audit log endpoints (read-only)
    app.MapAuditLogEndpoints();

    // Engine V1 API endpoints (versioned API surface)
    // Only map if EngineMode is enabled (defaults to true)
    // This allows Maemo to run as a headless Compliance Engine without UI-specific behavior
    // /api/... endpoints remain available for Maemo Portal / internal use
    // /engine/v1/... endpoints are for external consumable Compliance Engine
    var engineDeploymentContext = app.Services.GetRequiredService<IDeploymentContext>();
    if (engineDeploymentContext.EngineModeEnabled)
    {
        app.MapEngineV1();
        Log.Information("Engine V1 API endpoints enabled (EngineMode:Enabled = true)");
    }
    else
    {
        Log.Information("Engine V1 API endpoints disabled (EngineMode:Enabled = false)");
    }

    // Admin V1 API endpoints - Internal platform operations for Codist staff
    // Only map if AdminMode is enabled (defaults to true)
    // /admin/v1/... endpoints require PlatformAdmin authorization (except in Development)
    var adminDeploymentContext = app.Services.GetRequiredService<IDeploymentContext>();
    if (adminDeploymentContext.AdminModeEnabled)
    {
        app.MapAdminV1(app.Environment);
        Log.Information("Admin V1 API endpoints enabled (AdminMode:Enabled = true)");
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapDevelopmentDemoSeedEndpoints();
    }
    else
    {
        Log.Information("Admin V1 API endpoints disabled (AdminMode:Enabled = false)");
    }

    // Seed demo data if enabled
    var seedDemoData = builder.Configuration.GetValue<bool>("Deployment:DemoData:SeedOnStartup", false);
    var isDevelopment = app.Environment.IsDevelopment();
    
    if (seedDemoData || isDevelopment)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<MaemoCompliance.Application.Demo.IDemoDataSeeder>();
            await seeder.SeedAsync();
            Log.Information("Demo data seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Demo data seeding failed - continuing startup");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
