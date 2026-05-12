using Maemo.Application.Admin.Logs.Dtos;
using MediatR;

namespace Maemo.Application.Admin.Logs.Queries;

/// <summary>
/// Query to retrieve audit trail for a specific entity.
/// </summary>
public class GetEntityAuditTrailQuery : IRequest<IReadOnlyList<BusinessAuditLogDto>>
{
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
}

