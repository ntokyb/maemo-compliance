using MaemoCompliance.Application.AuditLog.Dtos;
using MediatR;

namespace MaemoCompliance.Application.AuditLog.Queries;

public class GetAuditLogsQuery : IRequest<IReadOnlyList<AuditLogEntryDto>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? TenantId { get; set; } // Only for system admins
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
}

