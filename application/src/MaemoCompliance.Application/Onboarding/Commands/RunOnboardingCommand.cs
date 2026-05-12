using MaemoCompliance.Application.Onboarding.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Onboarding.Commands;

/// <summary>
/// Command to run the onboarding wizard for a tenant.
/// </summary>
public class RunOnboardingCommand : IRequest
{
    public OnboardingRequest Request { get; set; } = null!;
}

