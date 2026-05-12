namespace MaemoCompliance.Application.Tenants.Dtos;

public class ConnectMicrosoft365Request
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
    public string TenantId { get; set; } = null!;
    public string? SharePointSiteId { get; set; }
    public string? SharePointDriveId { get; set; }
}

