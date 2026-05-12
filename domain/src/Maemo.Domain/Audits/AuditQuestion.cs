using Maemo.Domain.Common;

namespace Maemo.Domain.Audits;

public class AuditQuestion : BaseEntity
{
    public Guid AuditTemplateId { get; set; }
    public string Category { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public int MaxScore { get; set; }
}

