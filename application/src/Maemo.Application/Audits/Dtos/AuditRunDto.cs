namespace Maemo.Application.Audits.Dtos;

public class AuditRunDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AuditTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AuditorUserId { get; set; }
}

