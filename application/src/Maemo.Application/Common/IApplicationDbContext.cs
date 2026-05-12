using Maemo.Domain.AuditLog;
using Maemo.Domain.Audits;
using Maemo.Domain.Documents;
using Maemo.Domain.Logging;
using Maemo.Domain.Ncrs;
using Maemo.Domain.Risks;
using Maemo.Domain.Mail;
using Maemo.Domain.Tenants;
using Maemo.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Common;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Department> Departments { get; }
    DbSet<TenantSettings> TenantSettings { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<Ncr> Ncrs { get; }
    DbSet<NcrStatusHistory> NcrStatusHistory { get; }
    DbSet<NcrRiskLink> NcrRiskLinks { get; }
    DbSet<Risk> Risks { get; }
    DbSet<User> Users { get; }
    DbSet<UserInvitation> UserInvitations { get; }
    DbSet<WelcomeEmail> WelcomeEmails { get; }
    DbSet<ConsultantTenantLink> ConsultantTenantLinks { get; }
    DbSet<AuditTemplate> AuditTemplates { get; }
    DbSet<AuditQuestion> AuditQuestions { get; }
    DbSet<AuditRun> AuditRuns { get; }
    DbSet<AuditAnswer> AuditAnswers { get; }
    DbSet<AuditLogEntry> AuditLogs { get; }
    
    // Logging entities
    DbSet<ErrorLog> ErrorLogs { get; }
    DbSet<ApiCallLog> ApiCallLogs { get; }
    DbSet<WorkerJobLog> WorkerJobLogs { get; }
    DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; }
    DbSet<BusinessAuditLog> BusinessAuditLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

