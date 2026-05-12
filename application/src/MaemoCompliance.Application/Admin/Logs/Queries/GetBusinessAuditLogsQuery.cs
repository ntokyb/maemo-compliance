using MaemoCompliance.Application.Admin.Logs.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Admin.Logs.Queries;

/// <summary>
/// Query to retrieve business audit logs with optional filtering.
/// </summary>
public class GetBusinessAuditLogsQuery : IRequest<IReadOnlyList<BusinessAuditLogDto>>
{
    public Guid? TenantId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public int Limit { get; set; } = 100;
}

