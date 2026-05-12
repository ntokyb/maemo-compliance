using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

/// <summary>
/// Command to destroy a document according to retention rules.
/// This is a controlled destruction workflow that marks documents as destroyed
/// rather than hard-deleting them, ensuring auditability and compliance.
/// </summary>
public class DestroyDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string Reason { get; set; } = null!;
}

