using MaemoCompliance.Portal.Api.Authentication;
using MaemoCompliance.Portal.Api.Portal;
using MaemoCompliance.Portal.Api.Public;
using MaemoCompliance.Portal.Api.Middleware;
using MaemoCompliance.Application;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Infrastructure;
using MaemoCompliance.Infrastructure.HealthChecks;
using MaemoCompliance.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;
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
    Log.Information("Starting Maemo Portal API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with security-focused settings
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogConfiguration.ConfigureSerilog(configuration, context.Configuration, services);
    });

    // Add services
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger/OpenAPI for Portal API only
    var swaggerDeploymentContext = new MaemoCompliance.Infrastructure.Common.DeploymentContext(builder.Configuration);
    var isGovOnPrem = swaggerDeploymentContext.Mode == MaemoCompliance.Domain.Common.DeploymentMode.GovOnPrem;
    
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Maemo Portal API",
            Version = "v1",
            Description = @"
# Maemo Portal API

The Maemo Portal API provides endpoints for the Maemo Portal web application.

## Authentication

All endpoints require JWT Bearer Token authentication for user-based access.

## Tenant Scoping

All operations are automatically scoped to the authenticated tenant.
Tenant context is derived from the user's claims.

## Portal Endpoints

The `/api` endpoints are designed for internal Maemo Portal use.
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

        // Apply security requirements - use a more specific approach to avoid conflicts
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
            }
        });
        
        // Ensure operation IDs are unique by using a more deterministic approach
        // The issue might be that Swashbuckle is finding duplicate matches when resolving security schemes
        options.EnableAnnotations();

        // Ensure unique schema IDs to prevent conflicts
        options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
        
        // Group endpoints by tags
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor?.DisplayName ?? "Default" });
        
        // Only include Portal API endpoints in Swagger
        options.DocInclusionPredicate((name, api) =>
        {
            var path = api.RelativePath ?? "";
            // Only include Portal endpoints and health checks
            return path.StartsWith("api/", StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith("health", StringComparison.OrdinalIgnoreCase);
        });
        
        // Ensure unique operation IDs to prevent conflicts
        // Generate truly unique IDs by including full path hash - no dictionary needed
        options.CustomOperationIds(apiDesc =>
        {
            var path = apiDesc.RelativePath ?? "";
            var method = apiDesc.HttpMethod ?? "GET";
            
            // Try to get the endpoint name from metadata (set via WithName())
            var endpointName = apiDesc.ActionDescriptor?.EndpointMetadata
                ?.OfType<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()
                .FirstOrDefault()?.EndpointName;
            
            // Create a unique hash from the full path + method combination
            var uniqueKey = $"{method}:{path}";
            var pathHash = Math.Abs(uniqueKey.GetHashCode(StringComparison.OrdinalIgnoreCase)).ToString("X8");
            
            if (!string.IsNullOrEmpty(endpointName))
            {
                // Use endpoint name + hash for uniqueness
                return $"{endpointName}_{pathHash}";
            }
            
            // Fallback: generate from path and method with hash
            var cleanPath = path
                .Replace("/", "_")
                .Replace("{", "")
                .Replace("}", "")
                .Replace(":", "")
                .Replace("-", "_")
                .Trim('_');
            
            return $"{method}_{cleanPath}_{pathHash}";
        });
        
        // Resolve conflicts when multiple endpoints have the same path/method
        // Use the first one - this handles true duplicates
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
        // RequireAdmin policy - user must have Admin role
        options.AddPolicy("RequireAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "Admin" || c.Value == "0"))));

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
    Log.Information("Maemo Portal API running in {Mode} mode", deploymentContext.Mode);

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
    var enableSwagger = !isGovOnPrem || app.Environment.IsDevelopment();
    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Maemo Portal API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Maemo Portal API";
            options.DefaultModelsExpandDepth(-1);
            options.DisplayRequestDuration();
        });
    }

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

    app.MapPublicSignupEndpoints();

    // Map Portal API endpoints
    app.MapPortalEndpoints();
    Log.Information("Portal API endpoints enabled");

    // Seed demo data if enabled (ONLY in Portal API)
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
