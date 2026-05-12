using Maemo.Domain.Ncrs;

namespace Maemo.Shared.Contracts.Engine.Ncrs;

/// <summary>
/// Request DTO for updating NCR status in the Engine API.
/// </summary>
public class UpdateNcrStatusRequest
{
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
}

