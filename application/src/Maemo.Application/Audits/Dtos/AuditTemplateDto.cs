namespace Maemo.Application.Audits.Dtos;

public class AuditTemplateDto
{
    public Guid Id { get; set; }
    public Guid ConsultantUserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

