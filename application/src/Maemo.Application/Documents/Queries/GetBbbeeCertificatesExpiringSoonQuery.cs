using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Query to get BBBEE certificates expiring within the specified number of days.
/// </summary>
public class GetBbbeeCertificatesExpiringSoonQuery : IRequest<IReadOnlyList<DocumentDto>>
{
    /// <summary>
    /// Number of days to look ahead (default: 90).
    /// </summary>
    public int Days { get; set; } = 90;
}

