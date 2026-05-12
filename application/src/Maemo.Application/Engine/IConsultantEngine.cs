using Maemo.Application.Consultants.Dtos;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine interface for Consultant operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface IConsultantEngine
{
    Task<ConsultantDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultantClientDto>> GetClientsAsync(CancellationToken cancellationToken = default);
}

