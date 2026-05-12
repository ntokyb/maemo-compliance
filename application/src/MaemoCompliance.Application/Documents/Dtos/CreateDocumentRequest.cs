using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Documents.Dtos;

public class CreateDocumentRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public PiiDataType PiiDataType { get; set; } = PiiDataType.None;
    public PersonalInformationType PersonalInformationType { get; set; } = PersonalInformationType.None;
    public PiiType PiiType { get; set; } = PiiType.None;
    public string? PiiDescription { get; set; }
    public int? PiiRetentionPeriodInMonths { get; set; }
    public DateTime? BbbeeExpiryDate { get; set; }
    public int? BbbeeLevel { get; set; }
    public DateTime? RetainUntil { get; set; }
    public bool IsRetentionLocked { get; set; }
    
    // File Plan metadata (National Archives compliance)
    public string? FilePlanSeries { get; set; }
    public string? FilePlanSubSeries { get; set; }
    public string? FilePlanItem { get; set; }
}

