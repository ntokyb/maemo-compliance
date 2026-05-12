using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AccessRequests;
using MaemoCompliance.Domain.AuditLog;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Common;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Logging;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Risks;
using MaemoCompliance.Domain.Security;
using MaemoCompliance.Domain.Mail;
using MaemoCompliance.Domain.Tenants;
using MaemoCompliance.Domain.Webhooks;
using MaemoCompliance.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace MaemoCompliance.Infrastructure.Persistence;

public class MaemoComplianceDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantProvider _tenantProvider;
    private static readonly AsyncLocal<Guid?> _currentTenantId = new();

    public MaemoComplianceDbContext(DbContextOptions<MaemoComplianceDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
        // Set the current tenant ID for query filters (per async context)
        _currentTenantId.Value = tenantProvider.GetCurrentTenantId();
    }

    public DbSet<AccessRequest> AccessRequests { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<TenantSettings> TenantSettings { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;
    public DbSet<Ncr> Ncrs { get; set; } = null!;
    public DbSet<NcrStatusHistory> NcrStatusHistory { get; set; } = null!;
    public DbSet<NcrRiskLink> NcrRiskLinks { get; set; } = null!;
    public DbSet<Risk> Risks { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserInvitation> UserInvitations { get; set; } = null!;
    public DbSet<WelcomeEmail> WelcomeEmails { get; set; } = null!;
    public DbSet<ConsultantTenantLink> ConsultantTenantLinks { get; set; } = null!;
    public DbSet<AuditTemplate> AuditTemplates { get; set; } = null!;
    public DbSet<AuditQuestion> AuditQuestions { get; set; } = null!;
    public DbSet<AuditRun> AuditRuns { get; set; } = null!;
    public DbSet<AuditAnswer> AuditAnswers { get; set; } = null!;
    public DbSet<AuditProgramme> AuditProgrammes { get; set; } = null!;
    public DbSet<AuditScheduleItem> AuditScheduleItems { get; set; } = null!;
    public DbSet<AuditFinding> AuditFindings { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLogs { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    
    // Logging entities
    public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
    public DbSet<ApiCallLog> ApiCallLogs { get; set; } = null!;
    public DbSet<WorkerJobLog> WorkerJobLogs { get; set; } = null!;
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; set; } = null!;
    public DbSet<BusinessAuditLog> BusinessAuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Infrastructure assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaemoComplianceDbContext).Assembly);

        // Apply global query filter for tenant-aware entities
        ApplyTenantQueryFilter(modelBuilder);
    }

    private void ApplyTenantQueryFilter(ModelBuilder modelBuilder)
    {
        // REMOVED: Hardcoded tenant query filter
        // Tenant filtering is now applied via extension methods in query handlers
        // See: Infrastructure.Persistence.QueryExtensions.ForTenant()
        // This allows dynamic tenant resolution from ITenantProvider at query execution time
        
        // Note: We intentionally do NOT apply global query filters here because:
        // 1. EF Core query filters cannot use method calls (like ITenantProvider.GetCurrentTenantId())
        // 2. Query filters are evaluated at model creation time, not query execution time
        // 3. We need dynamic tenant resolution based on request context
        
        // All queries for TenantOwnedEntity types MUST use .ForTenant(tenantProvider) extension method
    }
}

