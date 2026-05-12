using MaemoCompliance.Admin.Api.Admin;
using MaemoCompliance.Admin.Api.Middleware;
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
    Log.Information("Starting Maemo Admin API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog with security-focused settings
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        SerilogConfiguration.ConfigureSerilog(configuration, context.Configuration, services);
    });

    // Add services
    builder.Services.AddEndpointsApiExplorer();
    
    // Configure Swagger/OpenAPI for Admin API only
    var swaggerDeploymentContext = new MaemoCompliance.Infrastructure.Common.DeploymentContext(builder.Configuration);
    var isGovOnPrem = swaggerDeploymentContext.Mode == MaemoCompliance.Domain.Common.DeploymentMode.GovOnPrem;
    
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Maemo Admin API",
            Version = "v1",
            Description = @"
# Maemo Admin API

The Maemo Admin API provides endpoints for platform administration and management.

## Authentication

All endpoints require JWT Bearer Token authentication with PlatformAdmin role.

## Authorization

All endpoints require PlatformAdmin or CodistAdmin role.

## Admin Endpoints

The `/admin/v1` endpoints are for internal platform operations by Codist staff only.
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
            }
        });

        // Ensure unique schema IDs to prevent conflicts
        options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
        
        // Group endpoints by tags
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor?.DisplayName ?? "Default" });
        
        // Only include Admin API endpoints in Swagger
        options.DocInclusionPredicate((name, api) =>
        {
            var path = api.RelativePath ?? "";
            // Only include Admin endpoints and health checks
            return path.StartsWith("admin/", StringComparison.OrdinalIgnoreCase) ||
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
            policy.WithOrigins("http://localhost:4300")
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
        });

    // Add Authorization with role-based policies
    builder.Services.AddAuthorization(options =>
    {
        // PlatformAdmin policy - for Codist staff only, requires PlatformAdmin role or specific claim
        options.AddPolicy("PlatformAdmin", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => 
                    (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "PlatformAdmin" || c.Value == "CodistAdmin"))));
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
    Log.Information("Maemo Admin API running in {Mode} mode", deploymentContext.Mode);

    // Check if AdminMode is enabled
    if (!deploymentContext.AdminModeEnabled)
    {
        Log.Warning("Admin API endpoints are disabled (AdminMode:Enabled = false)");
        Log.Warning("Set Deployment:AdminMode:Enabled = true in configuration to enable Admin API");
    }

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
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Maemo Admin API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Maemo Admin API";
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
    
    // Logging middleware (after authentication, before endpoints)
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

    // Map Admin V1 API endpoints (only if AdminMode is enabled)
    if (deploymentContext.AdminModeEnabled)
    {
        app.MapAdminV1();
        Log.Information("Admin V1 API endpoints enabled (AdminMode:Enabled = true)");
    }
    else
    {
        Log.Warning("Admin V1 API endpoints disabled (AdminMode:Enabled = false)");
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
