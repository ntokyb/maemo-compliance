using MediatR;

namespace Maemo.Application.Audits.Commands;

public class UploadAuditEvidenceCommand : IRequest<string>
{
    public Guid AuditRunId { get; set; }
    public Guid AuditQuestionId { get; set; }
    public string FileName { get; set; } = null!;
    public Stream FileContent { get; set; } = null!;
}

