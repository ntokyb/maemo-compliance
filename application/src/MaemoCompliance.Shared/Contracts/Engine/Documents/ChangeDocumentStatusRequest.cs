using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Shared.Contracts.Engine.Documents;

/// <summary>
/// Request DTO for changing document status in the Engine API.
/// </summary>
public class ChangeDocumentStatusRequest
{
    public DocumentStatus Status { get; set; }
    public string? ApproverUserId { get; set; }
}

