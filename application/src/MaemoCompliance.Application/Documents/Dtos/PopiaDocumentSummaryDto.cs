using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Documents.Dtos;

public class PopiaDocumentSummaryDto
{
    public int TotalDocuments { get; set; }
    public int DocumentsWithNoPersonalInfo { get; set; }
    public int DocumentsWithPersonalInfo { get; set; }
    public int DocumentsWithSpecialPersonalInfo { get; set; }
    
    public List<PopiaDocumentSummaryByCategoryDto> ByCategory { get; set; } = new();
    public List<PopiaDocumentSummaryByDepartmentDto> ByDepartment { get; set; } = new();
    public List<PopiaDocumentSummaryByOwnerDto> ByOwner { get; set; } = new();
}

public class PopiaDocumentSummaryByCategoryDto
{
    public string Category { get; set; } = null!;
    public int Total { get; set; }
    public int WithNoPersonalInfo { get; set; }
    public int WithPersonalInfo { get; set; }
    public int WithSpecialPersonalInfo { get; set; }
}

public class PopiaDocumentSummaryByDepartmentDto
{
    public string Department { get; set; } = null!;
    public int Total { get; set; }
    public int WithNoPersonalInfo { get; set; }
    public int WithPersonalInfo { get; set; }
    public int WithSpecialPersonalInfo { get; set; }
}

public class PopiaDocumentSummaryByOwnerDto
{
    public string OwnerUserId { get; set; } = null!;
    public int Total { get; set; }
    public int WithNoPersonalInfo { get; set; }
    public int WithPersonalInfo { get; set; }
    public int WithSpecialPersonalInfo { get; set; }
}

