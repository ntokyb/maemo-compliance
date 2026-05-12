using Maemo.Application.Billing;
using Maemo.Application.Common;
using Maemo.Application.Demo;
using Maemo.Application.Security;
using Maemo.Application.Webhooks;
using Maemo.Domain.Common;
using Maemo.Infrastructure.AuditLog;
using Maemo.Infrastructure.Billing;
using Maemo.Infrastructure.Common;
using Maemo.Infrastructure.Demo;
using Maemo.Infrastructure.FileStorage;
using Maemo.Infrastructure.Graph;
using Maemo.Infrastructure.MultiTenancy;
using Maemo.Infrastructure.Persistence;
using Maemo.Infrastructure.Security;
using Maemo.Infrastructure.Retention;
using Maemo.Infrastructure.Storage;
using Maemo.Infrastructure.Templates;
using Maemo.Infrastructure.Mail;
using Maemo.Infrastructure.SharePoint;
using Maemo.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // HttpContextAccessor for accessing current HTTP context
        services.AddHttpContextAccessor();

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IBusinessAuditLogger, Maemo.Infrastructure.AuditLog.BusinessAuditLogger>();
        services.AddScoped<IRetentionPolicyService, RetentionPolicyService>();
        
        // Encryption service
        // In GovOnPrem mode, this is required and will throw if key is missing
        // In SaaS mode, encryption key may not be configured (optional)
        var deploymentContextForEncryption = new DeploymentContext(configuration);
        if (deploymentContextForEncryption.Mode == DeploymentMode.GovOnPrem)
        {
            // GovOnPrem mode requires encryption - register and validate key exists
            services.AddScoped<IEncryptionService, AesEncryptionService>();
        }
        else
        {
            // SaaS mode - register as optional (may be null)
            // This allows SaaS deployments to run without encryption key
            var encryptionKey = configuration["Security:EncryptionKey"];
            if (!string.IsNullOrWhiteSpace(encryptionKey))
            {
                services.AddScoped<IEncryptionService, AesEncryptionService>();
            }
        }
        
        // Deployment context (singleton since it's configuration-based)
        services.AddSingleton<IDeploymentContext>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return new DeploymentContext(config);
        });

        // Feature flags (singleton since it's configuration-based)
        services.AddSingleton<IFeatureFlags>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return new FeatureFlags(config);
        });

        // Multi-tenancy
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<Maemo.Application.Tenants.ITenantModuleChecker, TenantModuleChecker>();
        services.AddHttpContextAccessor(); // Required for TenantModuleChecker

        // Microsoft Graph integration (Phase 0: Stub implementation)
        services.AddScoped<IGraphService, GraphService>();
        services.AddScoped<ISharePointConnectionTester, SharePointConnectionTester>();

        // Billing provider (Phase 3: Stub implementation)
        services.AddScoped<IBillingProvider, PayFastBillingProvider>();
        services.AddSingleton<IPublicSignupRateLimiter, MemoryPublicSignupRateLimiter>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        // File storage service (legacy - kept for backward compatibility)
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IFileHashService, FileHashService>();

        // File storage provider (new unified interface)
        var deploymentContext = new DeploymentContext(configuration);
        var forceLocalStorage = configuration.GetValue("Storage:ForceLocalFileStorage", false);
        if (deploymentContext.Mode == DeploymentMode.GovOnPrem)
        {
            // GovOnPrem: Use local file storage only
            services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
        }
        else if (forceLocalStorage)
        {
            // Staging / dev SaaS: local disk without M365 Graph (see Storage:ForceLocalFileStorage)
            services.AddScoped<IFileStorageProvider, LocalFileStorageProvider>();
        }
        else
        {
            // SaaS: SharePoint via IFileStorageProvider (Graph upload must be configured for persistence)
            services.AddScoped<IFileStorageProvider, SharePointFileStorageProvider>();
        }

        // Database
        var connectionString = configuration.GetConnectionString("MaemoDatabase");
        services.AddDbContext<MaemoDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register IApplicationDbContext to use MaemoDbContext
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<MaemoDbContext>());

        // API Key service
        services.AddScoped<IApiKeyService, ApiKeyService>();

        // Webhook services
        services.AddHttpClient(); // For webhook dispatcher
        services.AddScoped<IWebhookSubscriptionService, WebhookSubscriptionService>();
        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();

        // Register demo data seeder
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();

        // Register onboarding seeder
        services.AddScoped<Maemo.Application.Onboarding.IOnboardingSeeder, Maemo.Infrastructure.Onboarding.OnboardingSeeder>();

        // Worker execution tracker (singleton for in-memory tracking)
        services.AddSingleton<Maemo.Application.Workers.IWorkerExecutionTracker, Maemo.Infrastructure.Workers.InMemoryWorkerExecutionTracker>();

        // Template library (singleton for caching)
        services.AddMemoryCache();
        services.AddSingleton<ITemplateLibrary, FileSystemTemplateLibrary>();

        return services;
    }
}

