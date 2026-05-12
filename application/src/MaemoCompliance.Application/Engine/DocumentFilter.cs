using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Filter criteria for listing documents.
/// </summary>
public class DocumentFilter
{
    public DocumentStatus? Status { get; set; }
    public string? Department { get; set; }
    public bool IncludeAllVersions { get; set; } = false;
}

