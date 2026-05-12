using MediatR;

namespace MaemoCompliance.Application.Users.Commands;

public sealed record CompleteUserOnboardingWizardCommand(
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone,
    string? OrganisationAddress,
    string? OrganisationName,
    string? LogoUrl,
    IReadOnlyList<string> Standards,
    IReadOnlyList<string>? TeamEmails) : IRequest;
