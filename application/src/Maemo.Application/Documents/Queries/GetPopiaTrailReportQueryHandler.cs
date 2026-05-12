using Maemo.Application.Common;
using Maemo.Application.Documents.Dtos;
using Maemo.Domain.AuditLog;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Handler for POPIA compliance trail report.
/// Returns who accessed documents containing personal data in the last N days.
/// </summary>
public class GetPopiaTrailReportQueryHandler : IRequestHandler<GetPopiaTrailReportQuery, IReadOnlyList<PopiaTrailReportItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetPopiaTrailReportQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<PopiaTrailReportItemDto>> Handle(GetPopiaTrailReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var cutoffDate = _dateTimeProvider.UtcNow.AddDays(-request.Days);

        // Get all Document.Accessed audit log entries for documents with PII data
        var auditLogs = await _context.BusinessAuditLogs
            .Where(log => 
                log.TenantId == tenantId &&
                log.Action == "Document.Accessed" &&
                log.EntityType == "Document" &&
                log.Timestamp >= cutoffDate)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync(cancellationToken);

        // Get document IDs from audit logs
        var documentIds = auditLogs
            .Select(log => Guid.Parse(log.EntityId))
            .Distinct()
            .ToList();

        // Load documents with PII data
        var documentsWithPii = await _context.Documents
            .Where(d => 
                d.TenantId == tenantId &&
                documentIds.Contains(d.Id) &&
                d.PiiDataType != PiiDataType.None)
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        // Build report items
        var reportItems = new List<PopiaTrailReportItemDto>();

        foreach (var auditLog in auditLogs)
        {
            var documentId = Guid.Parse(auditLog.EntityId);
            
            // Only include if document has PII data
            if (!documentsWithPii.TryGetValue(documentId, out var document))
            {
                continue;
            }

            // Extract PII data type from metadata if available, otherwise use document's PII data type
            var piiDataType = document.PiiDataType;
            if (!string.IsNullOrWhiteSpace(auditLog.MetadataJson))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(auditLog.MetadataJson);
                    if (metadata != null && metadata.TryGetValue("piiDataType", out var piiDataTypeValue))
                    {
                        if (Enum.TryParse<PiiDataType>(piiDataTypeValue.ToString(), out var parsedPiiDataType))
                        {
                            piiDataType = parsedPiiDataType;
                        }
                    }
                }
                catch
                {
                    // Use document's PII data type if metadata parsing fails
                }
            }

            reportItems.Add(new PopiaTrailReportItemDto
            {
                DocumentId = documentId,
                DocumentTitle = document.Title,
                PiiDataType = piiDataType,
                Department = document.Department,
                AccessedBy = auditLog.Username ?? auditLog.UserId,
                AccessedAt = auditLog.Timestamp
            });
        }

        return reportItems;
    }
}

