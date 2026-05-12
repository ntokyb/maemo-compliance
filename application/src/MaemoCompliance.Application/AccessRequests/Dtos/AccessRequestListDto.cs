namespace MaemoCompliance.Application.AccessRequests.Dtos;

public sealed class AccessRequestListDto
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = null!;
    public string Industry { get; init; } = null!;
    public string ContactName { get; init; } = null!;
    public string ContactEmail { get; init; } = null!;
    public string TargetStandardsSummary { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = null!;
}
