using Maemo.Domain.Common;

namespace Maemo.Domain.Mail;

/// <summary>
/// Queued welcome / transactional email (delivery stubbed until SMTP/SendGrid is wired).
/// </summary>
public class WelcomeEmail : BaseEntity
{
    public Guid TenantId { get; set; }
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public DateTime? SentAt { get; set; }
}
