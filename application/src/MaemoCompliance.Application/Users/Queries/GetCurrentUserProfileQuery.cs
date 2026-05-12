using MediatR;

namespace MaemoCompliance.Application.Users.Queries;

public sealed record UserProfileDto(
    string Email,
    string FullName,
    bool OnboardingComplete,
    string? JobTitle,
    string? Phone,
    string? AddressLine,
    IReadOnlyList<string> ComplianceStandards);

public sealed record GetCurrentUserProfileQuery : IRequest<UserProfileDto?>;
