using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Documents.Dtos;

/// <summary>
/// DTO representing a single entry in the POPIA trail report.
/// </summary>
public class PopiaTrailReportItemDto
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = null!;
    public PiiDataType PiiDataType { get; set; }
    public string? Department { get; set; }
    public string AccessedBy { get; set; } = null!;
    public DateTime AccessedAt { get; set; }
}

