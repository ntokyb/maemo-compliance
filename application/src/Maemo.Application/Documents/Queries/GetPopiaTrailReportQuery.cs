using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Query to get POPIA compliance trail report showing who accessed documents containing personal data.
/// </summary>
public class GetPopiaTrailReportQuery : IRequest<IReadOnlyList<PopiaTrailReportItemDto>>
{
    /// <summary>
    /// Number of days to look back (default: 30).
    /// </summary>
    public int Days { get; set; } = 30;
}

