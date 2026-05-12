using MaemoCompliance.Application.AccessRequests.Dtos;
using MediatR;

namespace MaemoCompliance.Application.AccessRequests.Queries;

/// <summary>Optional filter: Pending, Approved, Rejected (case-insensitive) or null for all.</summary>
public sealed record GetAccessRequestsQuery(string? Status) : IRequest<IReadOnlyList<AccessRequestListDto>>;
