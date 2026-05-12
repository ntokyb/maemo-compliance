using MediatR;

namespace MaemoCompliance.Application.Public.Commands;

public sealed record SubmitAccessRequestCommand(
    string CompanyName,
    string Industry,
    string CompanySize,
    string ContactName,
    string ContactEmail,
    string ContactRole,
    IReadOnlyList<string> TargetStandards,
    string ReferralSource,
    string? ClientIp) : IRequest<Guid>;
