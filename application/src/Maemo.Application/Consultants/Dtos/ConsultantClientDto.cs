namespace Maemo.Application.Consultants.Dtos;

public class ConsultantClientDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = null!;
    public string? Plan { get; set; }
    public bool IsActive { get; set; }
}

