using Maemo.Domain.Common;

namespace Maemo.Domain.Ncrs;

public class NcrStatusHistory : TenantOwnedEntity
{
    public Guid NcrId { get; set; }
    public NcrStatus OldStatus { get; set; }
    public NcrStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
}

