using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Documents.Dtos;

public class ChangeDocumentStatusRequest
{
    public DocumentStatus Status { get; set; }
    public string? ApproverUserId { get; set; }
}

