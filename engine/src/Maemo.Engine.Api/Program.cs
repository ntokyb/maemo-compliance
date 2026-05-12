using Maemo.Engine.Api.Authentication;
using Maemo.Engine.Api.Engine;
using Maemo.Engine.Api.Middleware;
using Maemo.Application;
using Maemo.Application.Common;
using Maemo.Infrastructure;
using Maemo.Infrastructure.HealthChecks;
using Maemo.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Maemo.Infrastructure.Logging;
using Serilog;
using System.Text.Json;

// Configure initial logger (will be replaced after full configuration)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Maemo Engine API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with security-focused settings
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogConfiguration.ConfigureSerilog(configuration, context.Configuration, services);
    });

    // Add services
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger/OpenAPI for Engine API only
    var swaggerDeploymentContext = new Maemo.Infrastructure.Common.DeploymentContext(builder.Configuration);
    var isGovOnPrem = swaggerDeploymentContext.Mode == Maemo.Domain.Common.DeploymentMode.GovOnPrem;
    
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Maemo Engine API",
            Version = "v1",
            Description = @"
# Maemo Engine API

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
        
        // Only include Engine API endpoints in Swagger
        options.DocInclusionPredicate((name, api) =>
        {
            var path = api.RelativePath ?? "";
            // Only include Engine endpoints and health checks
            return path.StartsWith("engine/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("health", StringComparison.OrdinalIgnoreCase);
        });
        
        // Ensure unique operation IDs to prevent conflicts
        options.CustomOperationIds(apiDesc =>
        {
            var endpointName = apiDesc.ActionDescriptor?.DisplayName;
            if (!string.IsNullOrEmpty(endpointName))
            {
                return endpointName.Replace(" ", "").Replace("/", "_").Replace("-", "_");
            }
            
            var path = apiDesc.RelativePath ?? "";
            var method = apiDesc.HttpMethod ?? "GET";
            var operationId = $"{method}_{path.Replace("/", "_").Replace("{", "").Replace("}", "").Replace(":", "").Replace("-", "_")}";
            return operationId;
        });
        
        // Resolve conflicts when multiple endpoints have the same path/method
        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        
        // Ignore obsolete actions to reduce conflicts
        options.IgnoreObsoleteActions();
        
        // Ignore obsolete properties in schemas
        options.IgnoreObsoleteProperties();
    });

    // Add FluentValidation
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngularDev", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
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
    var deploymentContext = app.Services.GetRequiredService<Maemo.Application.Common.IDeploymentContext>();
    Log.Information("Maemo Engine API running in {Mode} mode", deploymentContext.Mode);

    // Apply migrations in Development environment
    if (app.Environment.IsDevelopment())
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MaemoDbContext>();
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
    var enableSwagger = !isGovOnPrem || app.Environment.IsDevelopment();
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Maemo Engine API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Maemo Engine API";
            options.DefaultModelsExpandDepth(-1);
            options.DisplayRequestDuration();
        });
    }

    // Security headers middleware (before authentication)
    var deploymentContextForMiddleware = app.Services.GetRequiredService<Maemo.Application.Common.IDeploymentContext>();
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
        Predicate = _ => false,
        ResponseWriter = async (context, report) =>
        {
            var deploymentContext = app.Services.GetRequiredService<IDeploymentContext>();
            context.Response.ContentType = "application/json";
            
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

    // Simple health endpoint
    app.MapGet("/health", () =>
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

    // Map Engine V1 API endpoints
    app.MapEngineV1();
    Log.Information("Engine V1 API endpoints enabled");

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
