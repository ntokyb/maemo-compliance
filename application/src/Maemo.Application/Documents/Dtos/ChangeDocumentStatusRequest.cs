using Maemo.Domain.Documents;

namespace Maemo.Application.Documents.Dtos;

public class ChangeDocumentStatusRequest
{
    public DocumentStatus Status { get; set; }
    public string? ApproverUserId { get; set; }
}

