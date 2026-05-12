using Maemo.Application.Onboarding.Dtos;
using MediatR;

namespace Maemo.Application.Onboarding.Commands;

/// <summary>
/// Command to run the onboarding wizard for a tenant.
/// </summary>
public class RunOnboardingCommand : IRequest
{
    public OnboardingRequest Request { get; set; } = null!;
}

